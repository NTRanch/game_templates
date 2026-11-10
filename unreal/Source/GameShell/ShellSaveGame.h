// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/SaveGame.h"

#include "ShellSaveGame.generated.h"

/**
 * The save envelope. Every game save in this template is one of
 * these; there are no unversioned per-game SaveGame subclasses (see
 * SPEC.md, "WHAT NOT TO ADD"). Game-specific state travels as a
 * JSON string in PayloadJson, produced and consumed by the game
 * layer with the engine Json module.
 */
UCLASS()
class GAMESHELL_API UShellSaveGame : public USaveGame
{
	GENERATED_BODY()

public:
	/** Current envelope format. Bump on any persisted-shape change
	 *  and add the step to UShellSaveLibrary::MigrateSave. */
	static constexpr int32 CurrentFormatVersion = 1;

	/** Format this save was written with. Saves newer than
	 *  CurrentFormatVersion are refused by the save library. */
	UPROPERTY()
	int32 FormatVersion = CurrentFormatVersion;

	/** Seconds since the Unix epoch (UTC) when the save was written. */
	UPROPERTY()
	int64 SavedAtUnix = 0;

	/** Total play time at save time, in seconds. Owned by the game
	 *  layer; the shell stores and reports it, never computes it. */
	UPROPERTY()
	int64 PlaytimeSeconds = 0;

	/** Human-readable slot label shown in save/load UI. */
	UPROPERTY()
	FString Label;

	/** Game state as a JSON string. Additive fields first; any shape
	 *  change rides a FormatVersion bump and a migration step. */
	UPROPERTY()
	FString PayloadJson;
};
