// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#include "ShellGameMode.h"

AShellGameMode::AShellGameMode()
{
	// The shell's maps are single-player screens; seamless travel
	// would only hide load boundaries the template wants visible.
	bUseSeamlessTravel = false;
}
