// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"
#include "Blueprint/UserWidget.h"

#include "ShellPauseWidget.generated.h"

class UButton;
class UShellGameInstance;

/**
 * C++ base for the pause screen. The UMG layout is editor work:
 * create WBP_Pause with this class as parent (SPEC.md editor
 * steps). Buttons named ResumeButton, SaveButton, SettingsButton,
 * and QuitToTitleButton in the WBP are bound automatically when
 * present (BindWidgetOptional); the BlueprintCallable actions below
 * are the contract regardless.
 *
 * Saving from pause writes the "auto" slot through the save
 * library. The payload and label come from the game layer through
 * the two BlueprintImplementableEvents - the shell never invents
 * game state.
 */
UCLASS(Abstract, Blueprintable)
class GAMESHELL_API UShellPauseWidget : public UUserWidget
{
	GENERATED_BODY()

public:
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Pause")
	void OnResumeClicked();

	/** Saves to the "auto" slot using BuildSaveLabel() and
	 *  BuildSavePayloadJson(), then fires OnSaveCompleted(). */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Pause")
	void OnSaveClicked();

	UFUNCTION(BlueprintCallable, Category = "Game Shell|Pause")
	void OnSettingsClicked();

	/** Moves the flow to Title (unpaused) and fires
	 *  OnQuitToTitleRequested(); the game layer performs any
	 *  title-map travel from that event. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Pause")
	void OnQuitToTitleClicked();

	/** Game-layer seam: the JSON payload for the pause save. */
	UFUNCTION(BlueprintImplementableEvent, Category = "Game Shell|Pause")
	FString BuildSavePayloadJson();

	/** Game-layer seam: the label for the pause save. */
	UFUNCTION(BlueprintImplementableEvent, Category = "Game Shell|Pause")
	FString BuildSaveLabel();

	/** Game-layer seam: total play time in seconds for the save
	 *  header. Defaults to 0 when not implemented. */
	UFUNCTION(BlueprintImplementableEvent, Category = "Game Shell|Pause")
	int64 BuildPlaytimeSeconds();

	UFUNCTION(BlueprintImplementableEvent, Category = "Game Shell|Pause")
	void OnSaveCompleted(bool bSuccess);

	UFUNCTION(BlueprintImplementableEvent, Category = "Game Shell|Pause")
	void OnQuitToTitleRequested();

protected:
	virtual void NativeConstruct() override;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UButton> ResumeButton;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UButton> SaveButton;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UButton> SettingsButton;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UButton> QuitToTitleButton;

private:
	UShellGameInstance* GetShellGameInstance() const;
};
