// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "CoreMinimal.h"

/** Single log category for the whole shell. Every failure path in the
 *  template logs through this category; nothing fails silently. */
DECLARE_LOG_CATEGORY_EXTERN(LogGameShell, Log, All);
