// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"

#include "ShellTypes.generated.h"

/**
 * Top-level flow states for the shell. UShellGameInstance owns the
 * current state; widgets read it, they never assign it directly.
 */
UENUM(BlueprintType)
enum class EShellFlowState : uint8
{
	AgeGate,
	Title,
	Game,
	Paused
};

/** Where closing the settings screen returns the flow to. */
UENUM(BlueprintType)
enum class EShellSettingsReturnTo : uint8
{
	Title,
	Pause
};

/**
 * Canonical slot names. The save library accepts exactly these names
 * for game saves, and the settings subsystem owns the settings slot.
 * No other slot name is part of the contract.
 */
namespace ShellSlots
{
	inline const FString Settings = TEXT("ShellSettings");
	inline const FString Slot1 = TEXT("slot1");
	inline const FString Slot2 = TEXT("slot2");
	inline const FString Slot3 = TEXT("slot3");
	inline const FString Auto = TEXT("auto");

	/** SaveGame user index used by every shell save and load. */
	inline constexpr int32 UserIndex = 0;
}

/**
 * Player-facing shell settings. The shared shell contract:
 * every field has a default (the constructor values below) and a
 * clamp or repair rule enforced by Validate(). Settings loaded from
 * disk are untrusted input and always pass through Validate() (and
 * the NaN check in HasInvalidFloats()) before use.
 */
USTRUCT(BlueprintType)
struct GAMESHELL_API FShellSettings
{
	GENERATED_BODY()

	/** Window bounds, from the shared shell contract. */
	static constexpr int32 MinWindowWidth = 640;
	static constexpr int32 MaxWindowWidth = 7680;
	static constexpr int32 MinWindowHeight = 360;
	static constexpr int32 MaxWindowHeight = 4320;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	float MasterVolume = 1.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	float MusicVolume = 1.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	float SfxVolume = 1.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	bool bFullscreen = false;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	int32 WindowWidth = 1280;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	int32 WindowHeight = 720;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	bool bVSync = true;

	/** Accessibility: when true, the game layer suppresses flashing
	 *  effects. The shell only stores the flag; effects honour it. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	bool bReduceFlashing = false;

	/** Screen-shake intensity multiplier, 0 (off) to 1 (full). The
	 *  game layer scales its shake effects by this value. */
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	float ScreenShake = 1.0f;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Game Shell|Settings")
	bool bSubtitles = true;

	/**
	 * Clamps every field into its documented range and repairs NaN
	 * floats to their defaults. Idempotent; safe to call on any
	 * loaded-from-disk value before use. Real control flow, never
	 * assert-only, so the repair survives release builds.
	 */
	void Validate()
	{
		MasterVolume = SanitizeUnitInterval(MasterVolume, 1.0f);
		MusicVolume = SanitizeUnitInterval(MusicVolume, 1.0f);
		SfxVolume = SanitizeUnitInterval(SfxVolume, 1.0f);
		ScreenShake = SanitizeUnitInterval(ScreenShake, 1.0f);
		WindowWidth = FMath::Clamp(WindowWidth, MinWindowWidth, MaxWindowWidth);
		WindowHeight = FMath::Clamp(WindowHeight, MinWindowHeight, MaxWindowHeight);
	}

	/**
	 * True when any float field is NaN. A NaN on load is treated as
	 * corruption evidence by the settings subsystem (quarantine plus
	 * defaults), not silently repaired; Validate() is the repair path
	 * for values that are merely out of range.
	 */
	bool HasInvalidFloats() const
	{
		return FMath::IsNaN(MasterVolume) || FMath::IsNaN(MusicVolume)
			|| FMath::IsNaN(SfxVolume) || FMath::IsNaN(ScreenShake);
	}

private:
	static float SanitizeUnitInterval(float Value, float DefaultValue)
	{
		if (FMath::IsNaN(Value))
		{
			return DefaultValue;
		}
		return FMath::Clamp(Value, 0.0f, 1.0f);
	}
};

/** Header-only view of one save slot, filled by the save library. */
USTRUCT(BlueprintType)
struct GAMESHELL_API FShellSlotMetadata
{
	GENERATED_BODY()

	/** True when a slot file exists, even if it could not be read. */
	UPROPERTY(BlueprintReadOnly, Category = "Game Shell|Save")
	bool bExists = false;

	UPROPERTY(BlueprintReadOnly, Category = "Game Shell|Save")
	FString Label;

	/** Seconds since the Unix epoch (UTC) when the save was written. */
	UPROPERTY(BlueprintReadOnly, Category = "Game Shell|Save")
	int64 SavedAtUnix = 0;

	UPROPERTY(BlueprintReadOnly, Category = "Game Shell|Save")
	int64 PlaytimeSeconds = 0;
};
