// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"

#include "ShellGameMode.generated.h"

/**
 * Minimal GameMode for the title map: no pawn, no travel tricks.
 * Set it as the TitleMap's World Settings GameMode override
 * (SPEC.md editor steps). Gameplay maps override with their own
 * GameMode; nothing here constrains them.
 */
UCLASS()
class GAMESHELL_API AShellGameMode : public AGameModeBase
{
	GENERATED_BODY()

public:
	AShellGameMode();
};
