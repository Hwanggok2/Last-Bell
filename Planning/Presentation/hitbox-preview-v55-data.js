window.HITBOX_PREVIEW_SPEC = {
  "version": "hitbox-preview-v5.5",
  "status": "planning-preview-only",
  "referenceSize": [
    960,
    540
  ],
  "color": "#E9B85A",
  "fillAlpha": {
    "bright": 0.2,
    "dim": 0.04
  },
  "borderAlpha": {
    "bright": 0.62,
    "dim": 0.2
  },
  "borderPx": 2,
  "blinkCount": 3,
  "brightFraction": 0.5,
  "defaultImpactMs": 900,
  "fastImpactMs": 650,
  "flightMs": 120,
  "impactEffectMs": 260,
  "normalWindowMs": 160,
  "perfectWindowMs": 60,
  "recoveryMs": 260,
  "playerAnchor": [
    375,
    442
  ],
  "enemyAnchor": [
    675,
    442
  ],
  "enemyFrontX": 624,
  "playerBody": [
    353,
    340,
    44,
    100
  ],
  "crouchedBody": [
    351,
    364,
    54,
    76
  ],
  "dodgeOffsets": {
    "up": [
      0,
      -108
    ],
    "back": [
      -90,
      0
    ],
    "down": [
      0,
      0
    ],
    "forward": [
      90,
      0
    ]
  },
  "timingRule": "cueMs=singleImpactMs-120; each cue has three equal bright/dim periods; combo queues all cues first, then120ms flights with360/320ms impact gaps; ordinary160ms/perfect60ms measured separately for each impact",
  "combo": {
    "rule": "Preview ALL attacks in sequence, three full flashes each; only then execute attacks in the same order.",
    "defaultImpactGapMs": 360,
    "fastImpactGapMs": 320,
    "maxAttacks": 2,
    "completedCue": "outline alpha0.14 only; does not flash again",
    "defaultExampleMs": {
      "frontCue": [
        0,
        780
      ],
      "rearCue": [
        780,
        1560
      ],
      "frontFlight": [
        1560,
        1680
      ],
      "frontHit": 1680,
      "rearFlight": [
        1920,
        2040
      ],
      "rearHit": 2040
    }
  },
  "attacks": [
    {
      "id": "thrust",
      "name": "창 찌르기",
      "dodge": "back",
      "reviewLabel": "몸통 전체를 덮는 앞쪽 통로 · 후퇴",
      "rect": [
        340,
        306,
        284,
        136
      ],
      "anchorRule": "몬스터 전면 X부터 플레이어 몸통 뒤쪽 경계까지. 상단은 점프 회수 위치까지 포함.",
      "effect": "수평 창/충격 궤적이 몬스터 전면에서 플레이어 쪽으로 이동"
    },
    {
      "id": "low",
      "name": "낮은 휩쓸기",
      "dodge": "up",
      "reviewLabel": "발밑을 쓸어가는 띠 · 점프",
      "rect": [
        248,
        406,
        376,
        36
      ],
      "anchorRule": "플레이어 발밑 높이. 뒤로 한 걸음 영역까지 덮고 위쪽을 비운다.",
      "effect": "지면 가까운 수평 베기가 몬스터에서 플레이어 쪽으로 이동"
    },
    {
      "id": "high",
      "name": "높은 휩쓸기",
      "dodge": "down",
      "reviewLabel": "머리 높이의 띠 · 숙이기",
      "rect": [
        248,
        270,
        376,
        88
      ],
      "anchorRule": "서 있는 머리와 점프 위치를 덮고 숙인 머리 위에 최소4px 여백.",
      "effect": "높은 수평 베기가 몬스터에서 플레이어 쪽으로 이동"
    },
    {
      "id": "rear",
      "name": "후방 내려치기",
      "dodge": "forward",
      "reviewLabel": "등 뒤부터 현재 자리까지 · 전진",
      "rect": [
        248,
        218,
        165,
        224
      ],
      "anchorRule": "플레이어 뒤쪽과 현재 위치를 덮고 몬스터 쪽 빈 공간은 남긴다.",
      "effect": "몬스터가 보낸 궤적이 위로 돌아 등 뒤와 현재 자리에 낙하"
    }
  ],
  "visibility": {
    "directionText": false,
    "answerArrow": false,
    "preHighlightedCorrectButton": false,
    "timingBar": false,
    "normalDodgeControls": true,
    "simultaneousFlashingAreas": 1,
    "reducedFlash": "constant fill alpha0.12 and three corner contractions; no expanding waves, camera movement, trails or screen flash"
  },
  "geometryContract": "Rectangles are dangerous areas, never safe zones. Shared authored AttackShape and player dodge-body poses must drive both the visual and Core hit decision. No hidden required-direction failure for a dodge that actually leaves the shown footprint.",
  "previewScope": "v5.5 browser planning preview with screen-centered circular perfect waves extending beyond the viewport; no Unity combat changes.",
  "perfect": {
    "origin": "fixed center of the battle viewport, in screen space; independent of character position and world-camera shake",
    "ringMs": 420,
    "color": "#9EE3D4",
    "textMs": 320,
    "counterEligibilityMs": 500,
    "autoCounterDelayMs": 90,
    "returnMs": 0,
    "counterHitAfterInputMs": 200,
    "counterDurationMs": 500,
    "rule": "Only a confirmed perfect dodge unlocks snap-home on an accepted attack input. Reset root to battle anchor, then start existing approach/attack. No automatic damage, cooldown reset, extra invulnerability, queue cancellation or time dilation.",
    "originNormalized": [
      0.5,
      0.5
    ],
    "shape": "circle in screen pixels, equal radius on both axes",
    "ringDelayMs": [
      0,
      45
    ],
    "outerMarginPx": 64,
    "radiusFormula": "hypot(viewportWidth/2, viewportHeight/2)+64; both circles expand beyond all four corners",
    "drawLayer": "screen overlay after world camera transform is restored, before HUD",
    "reducedMotion": "static centered circle and label, no expansion"
  },
  "enemyVfx": {
    "clock": "combat clock; pause freezes poses, particles and all lifetimes",
    "warmup": "gold flecks gather at the weapon; brace then release in the final120ms",
    "release": "weapon trail120ms + directional streaks, faster final approach",
    "impact": "local gold/ivory core45ms + radial shards260ms + dust340ms + ground ripple300ms",
    "shake": {
      "maxPx": 3,
      "durationMs": 90,
      "hudMoves": false,
      "reducedMotion": false
    },
    "hitstopMs": 0,
    "particleBudget": 96,
    "sources": "authored reusable ParticleSystem/TrailRenderer prefabs in future Unity implementation; PerfectWave uses a screen-space overlay; deterministic canvas particles in this planning preview",
    "dangerVsDecoration": "danger rectangle is the full gameplay footprint; outlying sparks/dust/rings are decorative and do not deal damage",
    "palette": "enemy gold/amber; confirmed perfect mint/ivory",
    "audioPlan": "wind-up metal tension, release whoosh, impact low thump with metallic tick; not synthesized in this preview"
  }
};
