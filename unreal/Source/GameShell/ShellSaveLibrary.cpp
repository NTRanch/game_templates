// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#include "ShellSaveLibrary.h"

#include "GameShell.h"
#include "Kismet/GameplayStatics.h"
#include "Misc/DateTime.h"
#include "ShellFileUtil.h"

const TArray<FString>& UShellSaveLibrary::GetCanonicalSlots()
{
	static const TArray<FString> CanonicalSlots = {
		ShellSlots::Slot1, ShellSlots::Slot2, ShellSlots::Slot3, ShellSlots::Auto
	};
	return CanonicalSlots;
}

bool UShellSaveLibrary::IsCanonicalSlot(const FString& SlotName)
{
	return GetCanonicalSlots().Contains(SlotName);
}

bool UShellSaveLibrary::SaveShellGame(const FString& SlotName, const FString& Label,
	const FString& PayloadJson, int64 PlaytimeSeconds)
{
	if (!IsCanonicalSlot(SlotName))
	{
		UE_LOG(LogGameShell, Error,
			TEXT("SaveShellGame: '%s' is not a canonical slot (slot1..slot3, auto)."), *SlotName);
		return false;
	}

	if (PayloadJson.Len() > MaxPayloadChars)
	{
		UE_LOG(LogGameShell, Error,
			TEXT("SaveShellGame: payload for slot '%s' is %d chars, over the %d-char bound."),
			*SlotName, PayloadJson.Len(), MaxPayloadChars);
		return false;
	}

	UShellSaveGame* SaveObject = NewObject<UShellSaveGame>();
	SaveObject->FormatVersion = UShellSaveGame::CurrentFormatVersion;
	SaveObject->SavedAtUnix = FDateTime::UtcNow().ToUnixTimestamp();
	SaveObject->PlaytimeSeconds = PlaytimeSeconds;
	SaveObject->Label = Label;
	SaveObject->PayloadJson = PayloadJson;

	if (!UGameplayStatics::SaveGameToSlot(SaveObject, SlotName, ShellSlots::UserIndex))
	{
		UE_LOG(LogGameShell, Error,
			TEXT("SaveShellGame: engine failed to write slot '%s'."), *SlotName);
		return false;
	}
	return true;
}

UShellSaveGame* UShellSaveLibrary::LoadShellGame(const FString& SlotName)
{
	if (!IsCanonicalSlot(SlotName))
	{
		UE_LOG(LogGameShell, Error,
			TEXT("LoadShellGame: '%s' is not a canonical slot (slot1..slot3, auto)."), *SlotName);
		return nullptr;
	}

	USaveGame* RawSave = UGameplayStatics::LoadGameFromSlot(SlotName, ShellSlots::UserIndex);
	UShellSaveGame* Save = Cast<UShellSaveGame>(RawSave);
	if (Save == nullptr)
	{
		if (UGameplayStatics::DoesSaveGameExist(SlotName, ShellSlots::UserIndex))
		{
			UE_LOG(LogGameShell, Warning,
				TEXT("LoadShellGame: slot '%s' exists but failed to load; ")
				TEXT("quarantining the file and treating the slot as empty."),
				*SlotName);
			ShellFileUtil::QuarantineSlotFile(SlotName);
		}
		return nullptr;
	}

	if (Save->FormatVersion > UShellSaveGame::CurrentFormatVersion)
	{
		UE_LOG(LogGameShell, Error,
			TEXT("LoadShellGame: slot '%s' has format %d, newer than this build's %d; refused."),
			*SlotName, Save->FormatVersion, UShellSaveGame::CurrentFormatVersion);
		return nullptr;
	}

	if (Save->FormatVersion < UShellSaveGame::CurrentFormatVersion)
	{
		if (!MigrateSave(Save))
		{
			return nullptr;
		}
	}

	return Save;
}

bool UShellSaveLibrary::DoesShellSaveExist(const FString& SlotName)
{
	if (!IsCanonicalSlot(SlotName))
	{
		UE_LOG(LogGameShell, Error,
			TEXT("DoesShellSaveExist: '%s' is not a canonical slot (slot1..slot3, auto)."),
			*SlotName);
		return false;
	}
	return UGameplayStatics::DoesSaveGameExist(SlotName, ShellSlots::UserIndex);
}

bool UShellSaveLibrary::DeleteShellSave(const FString& SlotName)
{
	if (!IsCanonicalSlot(SlotName))
	{
		UE_LOG(LogGameShell, Error,
			TEXT("DeleteShellSave: '%s' is not a canonical slot (slot1..slot3, auto)."), *SlotName);
		return false;
	}
	return UGameplayStatics::DeleteGameInSlot(SlotName, ShellSlots::UserIndex);
}

FShellSlotMetadata UShellSaveLibrary::GetShellSlotMetadata(const FString& SlotName)
{
	FShellSlotMetadata Metadata;
	Metadata.bExists = DoesShellSaveExist(SlotName);
	if (!Metadata.bExists)
	{
		return Metadata;
	}

	// Deliberately not LoadShellGame(): metadata must also describe
	// saves this build refuses (newer format) without migrating or
	// quarantining anything as a side effect of a UI refresh.
	UShellSaveGame* Save = Cast<UShellSaveGame>(
		UGameplayStatics::LoadGameFromSlot(SlotName, ShellSlots::UserIndex));
	if (Save == nullptr)
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("GetShellSlotMetadata: slot '%s' exists but could not be read; ")
			TEXT("reporting existence only."),
			*SlotName);
		return Metadata;
	}

	Metadata.Label = Save->Label;
	Metadata.SavedAtUnix = Save->SavedAtUnix;
	Metadata.PlaytimeSeconds = Save->PlaytimeSeconds;
	return Metadata;
}

TArray<FString> UShellSaveLibrary::GetExistingSlots()
{
	TArray<FString> Existing;
	for (const FString& SlotName : GetCanonicalSlots())
	{
		if (UGameplayStatics::DoesSaveGameExist(SlotName, ShellSlots::UserIndex))
		{
			Existing.Add(SlotName);
		}
	}
	return Existing;
}

bool UShellSaveLibrary::MigrateSave(UShellSaveGame* Save)
{
	if (Save == nullptr)
	{
		UE_LOG(LogGameShell, Error, TEXT("MigrateSave: null save object."));
		return false;
	}

	int32 Steps = 0;
	while (Save->FormatVersion < UShellSaveGame::CurrentFormatVersion)
	{
		if (Steps >= MaxMigrationSteps)
		{
			UE_LOG(LogGameShell, Error,
				TEXT("MigrateSave: exceeded %d steps at format %d; refusing."),
				MaxMigrationSteps, Save->FormatVersion);
			return false;
		}

		switch (Save->FormatVersion)
		{
		case 0:
			// v0 saves predate the Label field; an empty label gets
			// the honest default rather than an invented one.
			if (Save->Label.IsEmpty())
			{
				Save->Label = TEXT("Untitled save");
			}
			Save->FormatVersion = 1;
			break;

		default:
			UE_LOG(LogGameShell, Error,
				TEXT("MigrateSave: no migration step from format %d."), Save->FormatVersion);
			return false;
		}

		++Steps;
	}

	return Save->FormatVersion == UShellSaveGame::CurrentFormatVersion;
}
