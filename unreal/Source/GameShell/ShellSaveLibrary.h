// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "ShellSaveGame.h"
#include "ShellTypes.h"

#include "ShellSaveLibrary.generated.h"

/**
 * The save contract. All game-save traffic goes through these
 * statics - Blueprint and C++ alike - so the envelope rules
 * (versioning, migration, corruption handling) exist in exactly one
 * place. Slots are the canonical names in ShellSlots (slot1..slot3,
 * auto); any other name is rejected with a logged error.
 *
 * Failure contract (no silent failures, no exceptions):
 *  - Save returns false on a bad slot name, an oversized payload,
 *    or an engine write failure (each logged).
 *  - Load returns nullptr for an empty slot, a corrupt slot (file
 *    quarantined to <slot>_corrupt.sav first), and a save whose
 *    FormatVersion is newer than this build (refused, logged, file
 *    left untouched - it belongs to a newer build).
 *  - Older formats are migrated in memory by MigrateSave(); the
 *    migrated form is written back on the next Save.
 */
UCLASS()
class GAMESHELL_API UShellSaveLibrary : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:
	/** Maximum PayloadJson length, in characters. Bounded by
	 *  contract: a save is a snapshot, not a database. */
	static constexpr int32 MaxPayloadChars = 1048576;

	/** Writes a new envelope save to a canonical slot. Stamps
	 *  SavedAtUnix from the current UTC time. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Save")
	static bool SaveShellGame(const FString& SlotName, const FString& Label,
		const FString& PayloadJson, int64 PlaytimeSeconds);

	/** Loads and (when older) migrates a save. Nullptr per the
	 *  failure contract above. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Save")
	static UShellSaveGame* LoadShellGame(const FString& SlotName);

	UFUNCTION(BlueprintPure, Category = "Game Shell|Save")
	static bool DoesShellSaveExist(const FString& SlotName);

	/** Deletes a slot. Returns false (logged) for a bad slot name;
	 *  deleting an absent slot is a successful no-op per the engine. */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Save")
	static bool DeleteShellSave(const FString& SlotName);

	/**
	 * Header metadata for one slot. Note the cost: the SaveGame
	 * system has no header-only read, so this loads the whole save
	 * object (payload included) and copies the header fields out.
	 * Fine for a title screen over four slots; not a per-frame API.
	 * A slot that exists but cannot be read reports bExists = true
	 * with default fields.
	 */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Save")
	static FShellSlotMetadata GetShellSlotMetadata(const FString& SlotName);

	/** The canonical slots that currently hold a save, in canonical
	 *  order (slot1, slot2, slot3, auto). */
	UFUNCTION(BlueprintCallable, Category = "Game Shell|Save")
	static TArray<FString> GetExistingSlots();

private:
	/** The canonical game-save slots, in canonical order. */
	static const TArray<FString>& GetCanonicalSlots();

	static bool IsCanonicalSlot(const FString& SlotName);

	/**
	 * Steps a save from its stored FormatVersion up to
	 * UShellSaveGame::CurrentFormatVersion, one version at a time.
	 * Returns false (logged) when a version has no step. Bounded:
	 * at most MaxMigrationSteps steps per load.
	 */
	static bool MigrateSave(UShellSaveGame* Save);

	static constexpr int32 MaxMigrationSteps = 8;
};
