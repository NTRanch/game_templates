// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#include "ShellGameInstance.h"

#include "GameShell.h"
#include "Kismet/GameplayStatics.h"
#include "Kismet/KismetSystemLibrary.h"
#include "ShellSettingsSubsystem.h"

void UShellGameInstance::Init()
{
	Super::Init();
	FlowState = bAgeGateEnabled ? EShellFlowState::AgeGate : EShellFlowState::Title;
	UE_LOG(LogGameShell, Log, TEXT("GameInstance flow starts at %s (age gate %s)."),
		bAgeGateEnabled ? TEXT("AgeGate") : TEXT("Title"),
		bAgeGateEnabled ? TEXT("enabled") : TEXT("disabled"));
}

UShellSettingsSubsystem* UShellGameInstance::GetSettingsSubsystem() const
{
	return GetSubsystem<UShellSettingsSubsystem>();
}

void UShellGameInstance::SetFlowState(EShellFlowState NewState)
{
	if (FlowState != NewState)
	{
		UE_LOG(LogGameShell, Log, TEXT("Flow state %d -> %d."),
			static_cast<int32>(FlowState), static_cast<int32>(NewState));
		FlowState = NewState;
	}
}

void UShellGameInstance::ConfirmAgeGate()
{
	if (FlowState != EShellFlowState::AgeGate)
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("ConfirmAgeGate called outside the AgeGate state; ignored."));
		return;
	}
	SetFlowState(EShellFlowState::Title);
}

void UShellGameInstance::GoToTitle()
{
	UGameplayStatics::SetGamePaused(this, false);
	SetFlowState(EShellFlowState::Title);
}

void UShellGameInstance::TransitionToGame(const FString& LevelName)
{
	UGameplayStatics::SetGamePaused(this, false);
	SetFlowState(EShellFlowState::Game);
	if (!LevelName.IsEmpty())
	{
		UGameplayStatics::OpenLevel(this, FName(*LevelName));
	}
}

void UShellGameInstance::PauseGame()
{
	if (FlowState != EShellFlowState::Game)
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("PauseGame called outside the Game state; ignored."));
		return;
	}
	UGameplayStatics::SetGamePaused(this, true);
	SetFlowState(EShellFlowState::Paused);
}

void UShellGameInstance::ResumeGame()
{
	if (FlowState != EShellFlowState::Paused)
	{
		UE_LOG(LogGameShell, Warning,
			TEXT("ResumeGame called outside the Paused state; ignored."));
		return;
	}
	UGameplayStatics::SetGamePaused(this, false);
	SetFlowState(EShellFlowState::Game);
}

void UShellGameInstance::OpenSettings(EShellSettingsReturnTo ReturnTo)
{
	SettingsReturnTo = ReturnTo;
	UE_LOG(LogGameShell, Log, TEXT("Settings opened (returns to %s)."),
		ReturnTo == EShellSettingsReturnTo::Pause ? TEXT("Pause") : TEXT("Title"));
}

void UShellGameInstance::CloseSettings()
{
	// The flow state never changed while settings was open, so the
	// only work here is restoring pause behaviour for the Pause
	// origin: PauseGame() is not re-run (state is already Paused),
	// but the pause is re-asserted in case a settings control
	// touched it.
	if (SettingsReturnTo == EShellSettingsReturnTo::Pause
		&& FlowState == EShellFlowState::Paused)
	{
		UGameplayStatics::SetGamePaused(this, true);
	}
	UE_LOG(LogGameShell, Log, TEXT("Settings closed."));
}

void UShellGameInstance::QuitShellGame()
{
	APlayerController* PlayerController = UGameplayStatics::GetPlayerController(this, 0);
	UKismetSystemLibrary::QuitGame(this, PlayerController, EQuitPreference::Quit, false);
}
