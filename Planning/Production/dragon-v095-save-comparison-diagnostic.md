# v0.9.5 native save comparison — confirmed string-comparison false positive

2026-10-25 KST. Read-only investigation of candidate01 `1280x720/fresh/fixtures/player-on-after/last-bell-save.json`. No production, test/probe or QA evidence changes.

## Established evidence

- Envelope payload SHA256 verifies. Repository Load returned without exception; no backup file exists for this fixture.
- The complete saved payload equals the last rendered `save` object in v6-state.jsonl. HP13647000 milli and enemyHP999974000 milli preserve the expected ordinary10 plus independent16 damage. Independent contact is completed (`skillContactPending=false`, remaining0), cooldown remains19356.571599841118ms and clock643.4284001588821ms.
- Failure occurs at DragonThemeSmokeProbe.V6.cs:148, raw `JsonUtility.ToJson(before)==JsonUtility.ToJson(disk)`, **before** constructing the restored GameSession. Therefore new Core migration/restore logic has not yet run at this failure point.
- Repository Read only verifies the unchanged payload checksum, deserializes and normalizes empty optional records. Idle and pending cases already passed the same comparison with integer-valued timing. The failed case introduces fractional clock/cooldown values.
- Candidate01 did not originally persist the loaded DTO/string. Root subsequently executed `DragonV095ClientValidation/roundtrip-readonly-diagnostic.cs.txt` in Unity and captured both strings in `DragonV095ClientValidation/roundtrip-readonly-result.json`. This supplies the previously missing evidence without running gameplay or changing the save.

## Confirmed field comparison

Recursive comparison of the complete parsed original and loaded JSON found exactly one difference:

| Field | Original | Loaded | Difference |
|---|---:|---:|---:|
| `$.run.skillCooldownRemainingMs` | 19356.571599841118 ms | 19356.57159984112 ms | 1 ULP / 3.637978807091713e-12 ms |

All other fields, types, object key sets, array lengths and ordered array members match exactly. `recovered=false`. The result is recorded in `DragonV095ClientValidation/roundtrip-field-diff.json`; the unchanged input evidence SHA256 is `9216faba83c860cdb5d06954801426ac92717c7ce4528689f885bcb430a8f259`.

This failure is caused by demanding identical JSON text across JsonUtility's floating-point parse/re-serialization. It is not evidence of lost cooldown, altered HP, repeated skill contact or save migration failure. The original failed QA assertion remains historical evidence; this diagnosis does not retroactively mark the entire native campaign passed.

## QA-owned comparison correction

Replace only the raw DTO JSON string-equality assertion with a recursive field comparison. Preserve checksum verification against the original payload bytes and the expected `recoveredBackup` result. QA/root owns the probe change; this client has not edited it.

- Compare integer/long amounts, stats, RNG states, counters, IDs, strings, booleans, flags, field presence, types, array lengths and every ordered array member exactly. Preserve null versus empty distinctions.
- For an explicit whitelist of finite time doubles, accept exact equality, or both **ULP distance <= 2 AND absolute difference <= 1e-6 ms**. Keep zero and negative sentinel values exact. Use a sign-aware ULP calculation; do not apply the tolerance to non-finite values. The absolute bound is 100 times smaller than the Core timing epsilon of 0.0001 ms.
- Whitelist: `run.clockMs`, `dodgeStartMs`, `attackReadyAtMs`, `attackHitAtMs`, `enemyHitAtMs`, `telegraphStartMs`, `stunUntilMs`, `immunityUntilMs`, `skillCooldownRemainingMs`, `skillContactRemainingMs`, `skillShieldRemainingMs`, `practiceRecoveryUntilMs`; `run.transition.elapsedMs`; and `run.companions[i].remainingMs`. The unqualified names in this list are all direct `run` fields. Apply this rule only to fields whose DTO type is double.
- Other floating-point fields, including `damageFraction`, `damageRemainder`, `attackSpeed` and legacy stat values, stay exact for this proven case. Any extension needs a recorded field-specific discrepancy and rationale.
- Do not round all numbers, omit fields or arrays, or allow a general floating-point tolerance. On failure, capture both JSON strings and all differing paths, values, absolute differences and ULP distances.
- Retain the domain/Core validation and assertions for exact HP/wallet/character state, pending contacts, RNG/reserved skill choices, no duplicate contact/reward and no revival. Only timer equality receives the narrow rule above.

## Freeze verification

All 31 files in `DragonV095ClientValidation/source-hashes.json` still match their frozen hashes. Manifest SHA256: `507f805f3a1167ea4c446df4b08bb01328595dab77ce1c761fb022501a05f2ab`. Runtime/Core/Data/Persistence, audio/art, scenes, probes and QA evidence are unchanged by this diagnostic work. No Unity execution, new Core tests, product build or full QA rerun was performed by this client; only the supplied output and source hashes were checked.
