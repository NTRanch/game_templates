// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#include "ShellSettingsSubsystem.h"

#include "AudioDevice.h"
#include "GameFramework/GameUserSettings.h"
#include "GameShell.h"
#include "Kismet/GameplayStatics.h"
#include "ShellFileUtil.h"
#include "ShellSettingsSave.h"
#include "Sound/SoundClass.h"

void UShellSettingsSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	LoadSettings();
}

void UShellSettingsSubsystem::Deinitialize()
{
	Super::Deinitialize();
}

void UShellSettingsSubsystem::LoadSettings()
{
	// Defaults first: every failure path below leaves valid settings.
	Settings = FShellSettings();

	USaveGame* RawSave = UGameplayStatics::LoadGameFromSlot(
		ShellSlots::Settings, ShellSlots::UserIndex);
	UShellSettingsSave* Loaded = Cast<UShellSettingsSave>(RawSave);
	if (Loaded == nullptr)
	{
		if (UGameplayStatics::DoesSaveGameExist(ShellSlots::Settings, ShellSlots::UserIndex))
		{
			UE_LOG(LogGameShell, Warning,
				TEXT("Settings slot exists but failed to load as UShellSettingsSave; ")
				TEXT("quarantining the file and using defaults."));
			ShellFileUtil::QuarantineSlotFile(ShellSlots::Settings);
		}
		else
		{
			UE_LOG(LogGameShell, Log,
				TEXT("No settings slot found; using defaults."));
		}
		return;
	}

	if (Loaded->SettingsFormatVersion > UShellSettingsSave::CurrentSettingsFormatVersion)
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("Settings format %d is newer than this build's %d; using defaults ")
			TEXT("and leaving the slot untouched."),
			Loaded->SettingsFormatVersion, UShellSettingsSave::CurrentSettingsFormatVersion);
		return;
	}

	if (Loaded->Settings.HasInvalidFloats())
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("Settings contain NaN values; quarantining the file and using defaults."));
		ShellFileUtil::QuarantineSlotFile(ShellSlots::Settings);
		return;
	}

	Settings = Loaded->Settings;
	Settings.Validate();
	UE_LOG(LogGameShell, Log, TEXT("Settings loaded from slot '%s'."), *ShellSlots::Settings);
}

void UShellSettingsSubsystem::SaveSettings()
{
	UShellSettingsSave* SaveObject = NewObject<UShellSettingsSave>(this);
	SaveObject->Settings = Settings;
	SaveObject->SettingsFormatVersion = UShellSettingsSave::CurrentSettingsFormatVersion;

	if (!UGameplayStatics::SaveGameToSlot(SaveObject, ShellSlots::Settings, ShellSlots::UserIndex))
	{
		UE_LOG(LogGameShell, Error,
			TEXT("Failed to write settings slot '%s'."), *ShellSlots::Settings);
	}
}

void UShellSettingsSubsystem::SetMasterVolume(float NewVolume)
{
	Settings.MasterVolume = NewVolume;
	Settings.Validate();
	SaveSettings();
}

void UShellSettingsSubsystem::SetMusicVolume(float NewVolume)
{
	Settings.MusicVolume = NewVolume;
	Settings.Validate();
	SaveSettings();
}

void UShellSettingsSubsystem::SetSfxVolume(float NewVolume)
{
	Settings.SfxVolume = NewVolume;
	Settings.Validate();
	SaveSettings();
}

void UShellSettingsSubsystem::SetFullscreen(bool bNewFullscreen)
{
	Settings.bFullscreen = bNewFullscreen;
	Settings.Validate();
	SaveSettings();
	ApplyDisplaySettings();
}

void UShellSettingsSubsystem::SetResolution(int32 NewWidth, int32 NewHeight)
{
	Settings.WindowWidth = NewWidth;
	Settings.WindowHeight = NewHeight;
	Settings.Validate();
	SaveSettings();
	ApplyDisplaySettings();
}

void UShellSettingsSubsystem::SetVSync(bool bNewVSync)
{
	Settings.bVSync = bNewVSync;
	Settings.Validate();
	SaveSettings();
	ApplyDisplaySettings();
}

void UShellSettingsSubsystem::SetReduceFlashing(bool bNewReduceFlashing)
{
	Settings.bReduceFlashing = bNewReduceFlashing;
	Settings.Validate();
	SaveSettings();
}

void UShellSettingsSubsystem::SetScreenShake(float NewScreenShake)
{
	Settings.ScreenShake = NewScreenShake;
	Settings.Validate();
	SaveSettings();
}

void UShellSettingsSubsystem::SetSubtitles(bool bNewSubtitles)
{
	Settings.bSubtitles = bNewSubtitles;
	Settings.Validate();
	SaveSettings();
}

void UShellSettingsSubsystem::ApplyDisplaySettings()
{
	UGameUserSettings* UserSettings = UGameUserSettings::GetGameUserSettings();
	if (UserSettings == nullptr)
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("ApplyDisplaySettings: UGameUserSettings unavailable; display settings stored only."));
		return;
	}

	UserSettings->SetFullscreenMode(
		Settings.bFullscreen ? EWindowMode::Fullscreen : EWindowMode::Windowed);
	UserSettings->SetScreenResolution(FIntPoint(Settings.WindowWidth, Settings.WindowHeight));
	UserSettings->SetVSyncEnabled(Settings.bVSync);
	UserSettings->ApplySettings(false);
}

void UShellSettingsSubsystem::ApplyAudioVolumes(
	USoundClass* MasterClass, USoundClass* MusicClass, USoundClass* SfxClass)
{
	if (MasterClass == nullptr && MusicClass == nullptr && SfxClass == nullptr)
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("ApplyAudioVolumes: all SoundClass assets are null; volumes are stored ")
			TEXT("but not applied. Create the classes per SPEC.md and pass them in."));
		return;
	}

	if (GEngine == nullptr)
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("ApplyAudioVolumes: GEngine unavailable; volumes stored only."));
		return;
	}

	// API note: GEngine->GetMainAudioDevice() returns an
	// FAudioDeviceHandle in UE5, and FAudioDevice::SetSoundClassVolume
	// is the runtime per-class volume route. This template is
	// text-authored and has never been compiled; verify both against
	// the installed 5.8 headers at first compile (SPEC.md, GATES).
	FAudioDeviceHandle AudioDevice = GEngine->GetMainAudioDevice();
	if (!AudioDevice.IsValid())
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("ApplyAudioVolumes: no main audio device; volumes stored only."));
		return;
	}

	if (MasterClass != nullptr)
	{
		AudioDevice->SetSoundClassVolume(MasterClass, Settings.MasterVolume);
	}
	if (MusicClass != nullptr)
	{
		AudioDevice->SetSoundClassVolume(MusicClass, Settings.MusicVolume);
	}
	if (SfxClass != nullptr)
	{
		AudioDevice->SetSoundClassVolume(SfxClass, Settings.SfxVolume);
	}
}
