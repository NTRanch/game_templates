// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#include "ShellAgeGateWidget.h"

#include "Components/Button.h"
#include "Kismet/KismetSystemLibrary.h"
#include "ShellGameInstance.h"

UShellGameInstance* UShellAgeGateWidget::GetShellGameInstance() const
{
	return Cast<UShellGameInstance>(GetGameInstance());
}

void UShellAgeGateWidget::NativeConstruct()
{
	Super::NativeConstruct();

	if (ConfirmButton != nullptr)
	{
		ConfirmButton->OnClicked.AddDynamic(this, &UShellAgeGateWidget::OnConfirmClicked);
	}
	if (DeclineButton != nullptr)
	{
		DeclineButton->OnClicked.AddDynamic(this, &UShellAgeGateWidget::OnDeclineClicked);
	}
}

void UShellAgeGateWidget::OnConfirmClicked()
{
	if (UShellGameInstance* GameInstance = GetShellGameInstance())
	{
		GameInstance->ConfirmAgeGate();
	}
}

void UShellAgeGateWidget::OnDeclineClicked()
{
	UKismetSystemLibrary::QuitGame(this, GetOwningPlayer(), EQuitPreference::Quit, false);
}
