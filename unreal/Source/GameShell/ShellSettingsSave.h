// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/SaveGame.h"
#include "ShellTypes.h"

#include "ShellSettingsSave.generated.h"

/**
 * On-disk form of FShellSettings, stored in the slot named by
 * ShellSlots::Settings through the engine SaveGame system.
 * Settings have their own format version, independent of the game
 * save envelope version in UShellSaveGame.
 */
UCLASS()
class GAMESHELL_API UShellSettingsSave : public USaveGame
{
	GENERATED_BODY()

public:
	/** Current settings format. Bump and migrate in the settings
	 *  subsystem when the persisted shape changes. */
	static constexpr int32 CurrentSettingsFormatVersion = 1;

	UPROPERTY()
	int32 SettingsFormatVersion = CurrentSettingsFormatVersion;

	UPROPERTY()
	FShellSettings Settings;
};
