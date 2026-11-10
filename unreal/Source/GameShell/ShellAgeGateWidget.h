// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"
#include "Blueprint/UserWidget.h"

#include "ShellAgeGateWidget.generated.h"

class UButton;
class UShellGameInstance;

/**
 * C++ base for the age gate. The UMG layout is editor work: create
 * WBP_AgeGate with this class as parent (SPEC.md editor steps).
 * Buttons named ConfirmButton and DeclineButton in the WBP are
 * bound automatically when present (BindWidgetOptional); the
 * BlueprintCallable actions below are the contract regardless.
 *
 * The gate exists only when UShellGameInstance::bAgeGateEnabled is
 * true (off by default). Confirm moves the flow to Title; Decline
 * quits the game. There is no third path by design.
 */
UCLASS(Abstract, Blueprintable)
class GAMESHELL_API UShellAgeGateWidget : public UUserWidget
{
	GENERATED_BODY()

public:
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Age Gate")
	void OnConfirmClicked();

	/** Quits via UKismetSystemLibrary::QuitGame. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Age Gate")
	void OnDeclineClicked();

protected:
	virtual void NativeConstruct() override;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UButton> ConfirmButton;

	UPROPERTY(meta = (BindWidgetOptional))
	TObjectPtr<UButton> DeclineButton;

private:
	UShellGameInstance* GetShellGameInstance() const;
};
