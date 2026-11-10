// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#include "ShellTitleWidget.h"

#include "Components/Button.h"
#include "GameShell.h"
#include "ShellGameInstance.h"
#include "ShellSaveGame.h"
#include "ShellSaveLibrary.h"

UShellGameInstance* UShellTitleWidget::GetShellGameInstance() const
{
	return Cast<UShellGameInstance>(GetGameInstance());
}

void UShellTitleWidget::NativeConstruct()
{
	Super::NativeConstruct();

	if (NewGameButton != nullptr)
	{
		NewGameButton->OnClicked.AddDynamic(this, &UShellTitleWidget::OnNewGameClicked);
	}
	if (ContinueButton != nullptr)
	{
		ContinueButton->OnClicked.AddDynamic(this, &UShellTitleWidget::OnContinueClicked);
	}
	if (SettingsButton != nullptr)
	{
		SettingsButton->OnClicked.AddDynamic(this, &UShellTitleWidget::OnSettingsClicked);
	}
	if (QuitButton != nullptr)
	{
		QuitButton->OnClicked.AddDynamic(this, &UShellTitleWidget::OnQuitClicked);
	}

	RefreshContinueState();
}

void UShellTitleWidget::RefreshContinueState()
{
	bHasAnySave = UShellSaveLibrary::GetExistingSlots().Num() > 0;
}

void UShellTitleWidget::OnNewGameClicked()
{
	OnNewGameRequested();
}

void UShellTitleWidget::OnContinueClicked()
{
	// Newest readable save wins: scan the (at most four) canonical
	// slots by metadata timestamp, then load that slot in full.
	FString NewestSlot;
	int64 NewestSavedAt = -1;
	for (const FString& SlotName : UShellSaveLibrary::GetExistingSlots())
	{
		const FShellSlotMetadata Metadata = UShellSaveLibrary::GetShellSlotMetadata(SlotName);
		if (Metadata.SavedAtUnix > NewestSavedAt)
		{
			NewestSavedAt = Metadata.SavedAtUnix;
			NewestSlot = SlotName;
		}
	}

	if (NewestSlot.IsEmpty())
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("Continue clicked with no existing save slot; ignored."));
		return;
	}

	UShellSaveGame* Save = UShellSaveLibrary::LoadShellGame(NewestSlot);
	if (Save == nullptr)
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("Continue: newest slot '%s' could not be loaded; staying on title."),
			*NewestSlot);
		RefreshContinueState();
		return;
	}

	// The game layer restores state before the flow moves, so its
	// world setup can read the payload synchronously.
	OnSaveLoaded(Save);

	if (UShellGameInstance* GameInstance = GetShellGameInstance())
	{
		GameInstance->TransitionToGame(FString());
	}
}

void UShellTitleWidget::OnSettingsClicked()
{
	if (UShellGameInstance* GameInstance = GetShellGameInstance())
	{
		GameInstance->OpenSettings(EShellSettingsReturnTo::Title);
	}
}

void UShellTitleWidget::OnQuitClicked()
{
	if (UShellGameInstance* GameInstance = GetShellGameInstance())
	{
		GameInstance->QuitShellGame();
	}
}
