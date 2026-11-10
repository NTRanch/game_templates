// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"
#include "GameShell.h"
#include "HAL/FileManager.h"
#include "Misc/Paths.h"

/**
 * Internal helpers shared by the settings subsystem and the save
 * library. Not a UObject header: no reflection, no Blueprint surface,
 * header-only so both call sites share exactly one implementation.
 *
 * The engine's SaveGame system stores slot <Name> at
 * <ProjectSavedDir>/SaveGames/<Name>.sav. That path mapping is
 * engine behaviour this template relies on for byte-level
 * quarantine; SPEC.md lists verifying it as a first-bring-up check.
 */
namespace ShellFileUtil
{
	/** Absolute path of the .sav file backing a slot name. */
	inline FString GetSlotFilePath(const FString& SlotName)
	{
		return FPaths::ProjectSavedDir() / TEXT("SaveGames") / (SlotName + TEXT(".sav"));
	}

	/** Absolute path a quarantined slot file is copied to. */
	inline FString GetQuarantineFilePath(const FString& SlotName)
	{
		return FPaths::ProjectSavedDir() / TEXT("SaveGames") / (SlotName + TEXT("_corrupt.sav"));
	}

	/**
	 * Copies the slot's .sav beside itself as <Slot>_corrupt.sav so a
	 * corrupt save survives for inspection instead of being silently
	 * overwritten by the next save. Returns false (and logs) when
	 * there is nothing to copy or the copy fails; callers continue
	 * with their empty/defaults path either way. The original file is
	 * left in place; the copy is evidence, not a move.
	 */
	inline bool QuarantineSlotFile(const FString& SlotName)
	{
		const FString SourcePath = GetSlotFilePath(SlotName);
		IFileManager& FileManager = IFileManager::Get();
		if (!FileManager.FileExists(*SourcePath))
		{
			UE_LOG(LogGameShell, Verbose,
				TEXT("Quarantine: no file for slot '%s', nothing to copy."), *SlotName);
			return false;
		}

		const FString QuarantinePath = GetQuarantineFilePath(SlotName);
		if (FileManager.Copy(*QuarantinePath, *SourcePath, true, true) != COPY_OK)
		{
			UE_LOG(LogGameShell, Warning,
				TEXT("Quarantine: failed to copy slot '%s' to '%s'."),
				*SlotName, *QuarantinePath);
			return false;
		}

		UE_LOG(LogGameShell, Warning,
			TEXT("Quarantine: slot '%s' preserved as '%s'."), *SlotName, *QuarantinePath);
		return true;
	}
} // namespace ShellFileUtil
