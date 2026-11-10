// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#include "ShellSettingsWidget.h"

#include "Components/Button.h"
#include "Components/CheckBox.h"
#include "Components/Slider.h"
#include "ShellGameInstance.h"
#include "ShellSettingsSubsystem.h"
#include "ShellTypes.h"

UShellSettingsSubsystem* UShellSettingsWidget::GetSettingsSubsystem() const
{
	if (UShellGameInstance* GameInstance = GetShellGameInstance())
	{
		return GameInstance->GetSettingsSubsystem();
	}
	return nullptr;
}

UShellGameInstance* UShellSettingsWidget::GetShellGameInstance() const
{
	return Cast<UShellGameInstance>(GetGameInstance());
}

void UShellSettingsWidget::NativeConstruct()
{
	Super::NativeConstruct();

	if (MasterVolumeSlider != nullptr)
	{
		MasterVolumeSlider->OnValueChanged.AddDynamic(
			this, &UShellSettingsWidget::HandleMasterVolumeChanged);
	}
	if (MusicVolumeSlider != nullptr)
	{
		MusicVolumeSlider->OnValueChanged.AddDynamic(
			this, &UShellSettingsWidget::HandleMusicVolumeChanged);
	}
	if (SfxVolumeSlider != nullptr)
	{
		SfxVolumeSlider->OnValueChanged.AddDynamic(
			this, &UShellSettingsWidget::HandleSfxVolumeChanged);
	}
	if (ScreenShakeSlider != nullptr)
	{
		ScreenShakeSlider->OnValueChanged.AddDynamic(
			this, &UShellSettingsWidget::HandleScreenShakeChanged);
	}
	if (FullscreenCheckBox != nullptr)
	{
		FullscreenCheckBox->OnCheckStateChanged.AddDynamic(
			this, &UShellSettingsWidget::HandleFullscreenChanged);
	}
	if (VSyncCheckBox != nullptr)
	{
		VSyncCheckBox->OnCheckStateChanged.AddDynamic(
			this, &UShellSettingsWidget::HandleVSyncChanged);
	}
	if (SubtitlesCheckBox != nullptr)
	{
		SubtitlesCheckBox->OnCheckStateChanged.AddDynamic(
			this, &UShellSettingsWidget::HandleSubtitlesChanged);
	}
	if (ReduceFlashingCheckBox != nullptr)
	{
		ReduceFlashingCheckBox->OnCheckStateChanged.AddDynamic(
			this, &UShellSettingsWidget::HandleReduceFlashingChanged);
	}
	if (BackButton != nullptr)
	{
		BackButton->OnClicked.AddDynamic(this, &UShellSettingsWidget::OnBackClicked);
	}

	RefreshFromSettings();
}

void UShellSettingsWidget::RefreshFromSettings()
{
	UShellSettingsSubsystem* Subsystem = GetSettingsSubsystem();
	if (Subsystem == nullptr)
	{
		return;
	}
	const FShellSettings Settings = Subsystem->GetSettings();

	if (MasterVolumeSlider != nullptr)
	{
		MasterVolumeSlider->SetValue(Settings.MasterVolume);
	}
	if (MusicVolumeSlider != nullptr)
	{
		MusicVolumeSlider->SetValue(Settings.MusicVolume);
	}
	if (SfxVolumeSlider != nullptr)
	{
		SfxVolumeSlider->SetValue(Settings.SfxVolume);
	}
	if (ScreenShakeSlider != nullptr)
	{
		ScreenShakeSlider->SetValue(Settings.ScreenShake);
	}
	if (FullscreenCheckBox != nullptr)
	{
		FullscreenCheckBox->SetIsChecked(Settings.bFullscreen);
	}
	if (VSyncCheckBox != nullptr)
	{
		VSyncCheckBox->SetIsChecked(Settings.bVSync);
	}
	if (SubtitlesCheckBox != nullptr)
	{
		SubtitlesCheckBox->SetIsChecked(Settings.bSubtitles);
	}
	if (ReduceFlashingCheckBox != nullptr)
	{
		ReduceFlashingCheckBox->SetIsChecked(Settings.bReduceFlashing);
	}
}

void UShellSettingsWidget::ApplyVolumes()
{
	if (UShellSettingsSubsystem* Subsystem = GetSettingsSubsystem())
	{
		Subsystem->ApplyAudioVolumes(MasterSoundClass, MusicSoundClass, SfxSoundClass);
	}
}

void UShellSettingsWidget::SetMasterVolume(float NewVolume)
{
	if (UShellSettingsSubsystem* Subsystem = GetSettingsSubsystem())
	{
		Subsystem->SetMasterVolume(NewVolume);
		ApplyVolumes();
	}
}

void UShellSettingsWidget::SetMusicVolume(float NewVolume)
{
	if (UShellSettingsSubsystem* Subsystem = GetSettingsSubsystem())
	{
		Subsystem->SetMusicVolume(NewVolume);
		ApplyVolumes();
	}
}

void UShellSettingsWidget::SetSfxVolume(float NewVolume)
{
	if (UShellSettingsSubsystem* Subsystem = GetSettingsSubsystem())
	{
		Subsystem->SetSfxVolume(NewVolume);
		ApplyVolumes();
	}
}

void UShellSettingsWidget::SetScreenShake(float NewScreenShake)
{
	if (UShellSettingsSubsystem* Subsystem = GetSettingsSubsystem())
	{
		Subsystem->SetScreenShake(NewScreenShake);
	}
}

void UShellSettingsWidget::SetFullscreen(bool bNewFullscreen)
{
	if (UShellSettingsSubsystem* Subsystem = GetSettingsSubsystem())
	{
		Subsystem->SetFullscreen(bNewFullscreen);
	}
}

void UShellSettingsWidget::SetVSync(bool bNewVSync)
{
	if (UShellSettingsSubsystem* Subsystem = GetSettingsSubsystem())
	{
		Subsystem->SetVSync(bNewVSync);
	}
}

void UShellSettingsWidget::SetSubtitles(bool bNewSubtitles)
{
	if (UShellSettingsSubsystem* Subsystem = GetSettingsSubsystem())
	{
		Subsystem->SetSubtitles(bNewSubtitles);
	}
}

void UShellSettingsWidget::SetReduceFlashing(bool bNewReduceFlashing)
{
	if (UShellSettingsSubsystem* Subsystem = GetSettingsSubsystem())
	{
		Subsystem->SetReduceFlashing(bNewReduceFlashing);
	}
}

void UShellSettingsWidget::SetResolution(int32 NewWidth, int32 NewHeight)
{
	if (UShellSettingsSubsystem* Subsystem = GetSettingsSubsystem())
	{
		Subsystem->SetResolution(NewWidth, NewHeight);
	}
}

void UShellSettingsWidget::OnBackClicked()
{
	if (UShellGameInstance* GameInstance = GetShellGameInstance())
	{
		GameInstance->CloseSettings();
	}
}

void UShellSettingsWidget::HandleMasterVolumeChanged(float Value)
{
	SetMasterVolume(Value);
}

void UShellSettingsWidget::HandleMusicVolumeChanged(float Value)
{
	SetMusicVolume(Value);
}

void UShellSettingsWidget::HandleSfxVolumeChanged(float Value)
{
	SetSfxVolume(Value);
}

void UShellSettingsWidget::HandleScreenShakeChanged(float Value)
{
	SetScreenShake(Value);
}

void UShellSettingsWidget::HandleFullscreenChanged(bool bIsChecked)
{
	SetFullscreen(bIsChecked);
}

void UShellSettingsWidget::HandleVSyncChanged(bool bIsChecked)
{
	SetVSync(bIsChecked);
}

void UShellSettingsWidget::HandleSubtitlesChanged(bool bIsChecked)
{
	SetSubtitles(bIsChecked);
}

void UShellSettingsWidget::HandleReduceFlashingChanged(bool bIsChecked)
{
	SetReduceFlashing(bIsChecked);
}
