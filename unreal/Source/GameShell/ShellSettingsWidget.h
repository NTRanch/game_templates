// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"
#include "Blueprint/UserWidget.h"

#include "ShellSettingsWidget.generated.h"

class UButton;
class UCheckBox;
class USlider;
class USoundClass;
class UShellGameInstance;
class UShellSettingsSubsystem;

/**
 * C++ base for the settings screen. The UMG layout is editor work:
 * create WBP_Settings with this class as parent (SPEC.md editor
 * steps). Controls with these names in the WBP are bound
 * automatically when present (BindWidgetOptional); absence is
 * allowed and the BlueprintCallable setters below remain the
 * contract for any control the WBP graph wires itself:
 *   MasterVolumeSlider, MusicVolumeSlider, SfxVolumeSlider,
 *   ScreenShakeSlider (USlider); FullscreenCheckBox, VSyncCheckBox,
 *   SubtitlesCheckBox, ReduceFlashingCheckBox (UCheckBox);
 *   BackButton (UButton).
 *
 * Every control change flows into UShellSettingsSubsystem setters,
 * which validate, persist, and apply. Resolution has no bound
 * control in the base class (a resolution list is game-specific);
 * SetResolution() forwards to the subsystem, which validates and
 * applies it.
 *
 * The three SoundClass properties are assigned in the WBP's class
 * defaults after the editor creates the assets (SPEC.md); volume
 * changes call ApplyAudioVolumes() with them. Until they are
 * assigned, volumes are stored and persisted but not applied, and
 * the subsystem logs exactly that.
 */
UCLASS(Abstract, Blueprintable)
class GAMESHELL_API UShellSettingsWidget : public UUserWidget
{
	GENERATED_BODY()

public:
	/** Editor-created SoundClass assets (see SPEC.md). Assigned in
	 *  the WBP_Settings class defaults. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	TObjectPtr<USoundClass> MasterSoundClass;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	TObjectPtr<USoundClass> MusicSoundClass;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	TObjectPtr<USoundClass> SfxSoundClass;

	/** Pushes the persisted settings into every bound control. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void RefreshFromSettings();

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetMasterVolume(float NewVolume);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetMusicVolume(float NewVolume);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetSfxVolume(float NewVolume);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetScreenShake(float NewScreenShake);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetFullscreen(bool bNewFullscreen);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetVSync(bool bNewVSync);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetSubtitles(bool bNewSubtitles);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetReduceFlashing(bool bNewReduceFlashing);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetResolution(int32 NewWidth, int32 NewHeight);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void OnBackClicked();

protected:
	virtual void NativeConstruct() override;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<USlider> MasterVolumeSlider;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<USlider> MusicVolumeSlider;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<USlider> SfxVolumeSlider;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<USlider> ScreenShakeSlider;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UCheckBox> FullscreenCheckBox;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UCheckBox> VSyncCheckBox;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UCheckBox> SubtitlesCheckBox;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UCheckBox> ReduceFlashingCheckBox;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UButton> BackButton;

private:
	UFUNCTION()
	void HandleMasterVolumeChanged(float Value);

	UFUNCTION()
	void HandleMusicVolumeChanged(float Value);

	UFUNCTION()
	void HandleSfxVolumeChanged(float Value);

	UFUNCTION()
	void HandleScreenShakeChanged(float Value);

	UFUNCTION()
	void HandleFullscreenChanged(bool bIsChecked);

	UFUNCTION()
	void HandleVSyncChanged(bool bIsChecked);

	UFUNCTION()
	void HandleSubtitlesChanged(bool bIsChecked);

	UFUNCTION()
	void HandleReduceFlashingChanged(bool bIsChecked);

	UShellSettingsSubsystem* GetSettingsSubsystem() const;
	UShellGameInstance* GetShellGameInstance() const;
	void ApplyVolumes();
};
