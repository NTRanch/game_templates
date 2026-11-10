// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#include "ShellPauseWidget.h"

#include "Components/Button.h"
#include "ShellGameInstance.h"
#include "ShellSaveLibrary.h"
#include "ShellTypes.h"

UShellGameInstance* UShellPauseWidget::GetShellGameInstance() const
{
	return Cast<UShellGameInstance>(GetGameInstance());
}

void UShellPauseWidget::NativeConstruct()
{
	Super::NativeConstruct();

	if (ResumeButton != nullptr)
	{
		ResumeButton->OnClicked.AddDynamic(this, &UShellPauseWidget::OnResumeClicked);
	}
	if (SaveButton != nullptr)
	{
		SaveButton->OnClicked.AddDynamic(this, &UShellPauseWidget::OnSaveClicked);
	}
	if (SettingsButton != nullptr)
	{
		SettingsButton->OnClicked.AddDynamic(this, &UShellPauseWidget::OnSettingsClicked);
	}
	if (QuitToTitleButton != nullptr)
	{
		QuitToTitleButton->OnClicked.AddDynamic(this, &UShellPauseWidget::OnQuitToTitleClicked);
	}
}

void UShellPauseWidget::OnResumeClicked()
{
	if (UShellGameInstance* GameInstance = GetShellGameInstance())
	{
		GameInstance->ResumeGame();
	}
}

void UShellPauseWidget::OnSaveClicked()
{
	const bool bSuccess = UShellSaveLibrary::SaveShellGame(
		ShellSlots::Auto, BuildSaveLabel(), BuildSavePayloadJson(), BuildPlaytimeSeconds());
	OnSaveCompleted(bSuccess);
}

void UShellPauseWidget::OnSettingsClicked()
{
	if (UShellGameInstance* GameInstance = GetShellGameInstance())
	{
		GameInstance->OpenSettings(EShellSettingsReturnTo::Pause);
	}
}

void UShellPauseWidget::OnQuitToTitleClicked()
{
	if (UShellGameInstance* GameInstance = GetShellGameInstance())
	{
		GameInstance->GoToTitle();
	}
	OnQuitToTitleRequested();
}
