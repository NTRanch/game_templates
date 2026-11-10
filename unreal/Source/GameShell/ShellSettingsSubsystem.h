// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"
#include "ShellTypes.h"
#include "Subsystems/GameInstanceSubsystem.h"

#include "ShellSettingsSubsystem.generated.h"

class USoundClass;

/**
 * Owns FShellSettings for the running game: loads them at startup,
 * validates and persists every change, and applies what the engine
 * lets code apply.
 *
 * Persistence is the engine SaveGame system (slot
 * ShellSlots::Settings, user index ShellSlots::UserIndex). The
 * SaveGame system is the engine-native store for this template; it
 * does not offer the temp-file-plus-rename atomicity a hand-rolled
 * file store would, and SPEC.md records that mapping honestly.
 * Corruption handling:
 *  - slot missing: defaults, nothing logged above Log.
 *  - slot present but load fails / wrong class / NaN floats: the
 *    raw .sav is copied to ShellSettings_corrupt.sav (byte-level
 *    quarantine, see ShellFileUtil), a warning is logged, and
 *    defaults are used. The next successful SaveSettings()
 *    overwrites the bad slot with valid data.
 *  - format version newer than this build: values refused,
 *    defaults used, warning logged; the slot is NOT quarantined
 *    (it is not corrupt, it is from a newer build).
 *
 * Display application is real (UGameUserSettings). Audio volume
 * application needs the editor-created SoundClass assets and is
 * exposed as ApplyAudioVolumes(); see SPEC.md.
 */
UCLASS()
class GAMESHELL_API UShellSettingsSubsystem : public UGameInstanceSubsystem
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;

	/** Current settings (a copy; mutate through the setters so the
	 *  change is validated, saved, and applied). */
	UFUNCTION(BlueprintPure, Category = "Game Shell|Settings")
	FShellSettings GetSettings() const { return Settings; }

	/** Loads settings from the slot, applying the corruption rules
	 *  in the class comment. Called once from Initialize(); exposed
	 *  so a game can force a reload. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void LoadSettings();

	/** Persists the current settings to the slot. Called by every
	 *  setter; exposed for explicit saves after bulk changes. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SaveSettings();

	// Volume setters validate, save, and (for display setters) apply.
	// Volume values reach the audio device only through
	// ApplyAudioVolumes(), which needs the editor-created classes.
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetMasterVolume(float NewVolume);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetMusicVolume(float NewVolume);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetSfxVolume(float NewVolume);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetFullscreen(bool bNewFullscreen);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetResolution(int32 NewWidth, int32 NewHeight);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetVSync(bool bNewVSync);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetReduceFlashing(bool bNewReduceFlashing);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetScreenShake(float NewScreenShake);

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void SetSubtitles(bool bNewSubtitles);

	/** Pushes the display fields into UGameUserSettings and applies
	 *  them (fullscreen mode, resolution, VSync). */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void ApplyDisplaySettings();

	/**
	 * Pushes the three volume fields onto the given SoundClass
	 * assets through the main audio device. The classes are created
	 * in the Editor (SPEC.md editor steps) and passed in by the
	 * caller - typically UShellSettingsWidget, whose class
	 * properties a WBP subclass assigns. Any null class is skipped
	 * with a warning, never dereferenced.
	 */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Settings")
	void ApplyAudioVolumes(USoundClass* MasterClass, USoundClass* MusicClass, USoundClass* SfxClass);

private:
	FShellSettings Settings;
};
