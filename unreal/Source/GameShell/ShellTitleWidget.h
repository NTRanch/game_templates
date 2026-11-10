// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"
#include "Blueprint/UserWidget.h"

#include "ShellTitleWidget.generated.h"

class UButton;
class UShellGameInstance;
class UShellSaveGame;

/**
 * C++ base for the title screen. The UMG layout is editor work:
 * create WBP_Title with this class as parent (SPEC.md editor
 * steps). Buttons named NewGameButton, ContinueButton,
 * SettingsButton, and QuitButton in the WBP are bound automatically
 * when present (BindWidgetOptional - absence is allowed, the
 * BlueprintCallable actions below remain the contract and the WBP
 * graph may call them from any control instead).
 *
 * The C++ owns behaviour (flow transitions, save loading); the two
 * BlueprintImplementableEvents are the seams where the game layer
 * takes over (opening its first level, restoring a loaded save).
 */
UCLASS(Abstract, Blueprintable)
class GAMESHELL_API UShellTitleWidget : public UUserWidget
{
	GENERATED_BODY()

public:
	/** True when any canonical slot holds a save. Refreshed by
	 *  RefreshContinueState(); the WBP binds Continue's enabled
	 *  state to it. */
	UPROPERTY(BlueprintReadOnly, Category = "Game Shell|Title")
	bool bHasAnySave = false;

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Title")
	void RefreshContinueState();

	/** Fires OnNewGameRequested(); the game layer opens its first
	 *  level and calls TransitionToGame() on the GameInstance. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Title")
	void OnNewGameClicked();

	/** Loads the most recently written save (by SavedAtUnix),
	 *  fires OnSaveLoaded() with it, then moves the flow to Game.
	 *  A no-op (logged) when no readable save exists. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Title")
	void OnContinueClicked();

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Title")
	void OnSettingsClicked();

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Title")
	void OnQuitClicked();

	/** Game-layer seam: restore game state from the loaded save. */
	UFUNCTION(BlueprintImplementableEvent, Category = "Game Shell|Title")
	void OnSaveLoaded(UShellSaveGame* SaveGame);

	/** Game-layer seam: start a new game (open the first level). */
	UFUNCTION(BlueprintImplementableEvent, Category = "Game Shell|Title")
	void OnNewGameRequested();

protected:
	virtual void NativeConstruct() override;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UButton> NewGameButton;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UButton> ContinueButton;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UButton> SettingsButton;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UButton> QuitButton;

private:
	UShellGameInstance* GetShellGameInstance() const;
};
