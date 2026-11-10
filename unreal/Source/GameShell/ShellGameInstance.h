// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"
#include "Engine/GameInstance.h"
#include "ShellTypes.h"

#include "ShellGameInstance.generated.h"

class UShellSettingsSubsystem;

/**
 * Owns the shell's flow state and nothing else. Settings live in
 * UShellSettingsSubsystem, saves in UShellSaveLibrary; this class is
 * the state machine the widgets drive (SPEC.md, "WHAT NOT TO ADD":
 * no GameInstance god-object growth).
 *
 * Level travel is deliberately split: TransitionToGame() changes
 * state and optionally opens a level by name; quitting to title
 * changes state and unpauses, and the widget/game layer performs
 * any title-map travel through its own hooks. The shell never
 * hardcodes a level or asset path beyond what Config declares.
 */
UCLASS()
class GAMESHELL_API UShellGameInstance : public UGameInstance
{
	GENERATED_BODY()

public:
	virtual void Init() override;

	/**
	 * Age gate switch. Plain property with a default, per the shell
	 * contract: set it in a Blueprint child of this GameInstance or
	 * a config override in a shipping project. Off by default; when
	 * off, Init() starts the flow at Title and the age-gate widget
	 * is never shown.
	 */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Flow")
	bool bAgeGateEnabled = false;

	UFUNCTION(BlueprintPure, Category = "Game Shell|Flow")
	EShellFlowState GetFlowState() const { return FlowState; }

	/** The settings subsystem (loaded settings are available from
	 *  the moment the GameInstance exists). */
	UFUNCTION(BlueprintPure, Category = "Game Shell|Flow")
	UShellSettingsSubsystem* GetSettingsSubsystem() const;

	/** AgeGate -> Title. A no-op (logged) from any other state. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Flow")
	void ConfirmAgeGate();

	/** Any state -> Title, unpaused. Does not travel levels. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Flow")
	void GoToTitle();

	/** Any state -> Game, unpaused. When LevelName is non-empty it
	 *  is opened via UGameplayStatics::OpenLevel; when empty, only
	 *  the state changes (the game layer handles travel). */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Flow")
	void TransitionToGame(const FString& LevelName);

	/** Game -> Paused, via UGameplayStatics::SetGamePaused. A no-op
	 *  (logged) from any other state. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Flow")
	void PauseGame();

	/** Paused -> Game, unpaused. A no-op (logged) from any other
	 *  state. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Flow")
	void ResumeGame();

	/** Records where settings was opened from. The flow state does
	 *  not change: settings is an overlay on Title or Pause, and
	 *  CloseSettings() restores the pause behaviour for Pause. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Flow")
	void OpenSettings(EShellSettingsReturnTo ReturnTo);

	/** Closes the settings overlay. When it was opened from Pause,
	 *  the game stays paused; the pause widget layer re-shows
	 *  itself in response (Blueprint-side). */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Flow")
	void CloseSettings();

	/** Quits the game via UKismetSystemLibrary::QuitGame. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Flow")
	void QuitShellGame();

protected:
	UPROPERTY(BlueprintReadOnly, Category = "Game Shell|Flow")
	EShellFlowState FlowState = EShellFlowState::Title;

private:
	EShellSettingsReturnTo SettingsReturnTo = EShellSettingsReturnTo::Title;

	void SetFlowState(EShellFlowState NewState);
};
