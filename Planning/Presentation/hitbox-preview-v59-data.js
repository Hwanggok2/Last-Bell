window.HITBOX_PREVIEW_SPEC = {
  "version": "hitbox-preview-v5.9.1",
  "status": "runtime-aligned-preview",
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
  "defaultImpactMs": 720,
  "fastImpactMs": 720,
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
  "timingRule": "beat=singleImpact/3; cue i starts at i*3beat; impact i=count*3beat+i*2beat; flight=beat/2; all three bright pulses complete before first release",
  "combo": {
    "rule": "Preview ALL attacks in sequence, three full flashes each; only then execute attacks in the same order.",
    "defaultImpactGapMs": 480,
    "fastImpactGapMs": 480,
    "maxAttacks": 3,
    "completedCue": "outline alpha0.14 only; does not flash again",
    "defaultExampleMs": {
      "frontCue": [
        0,
        720
      ],
      "rearCue": [
        720,
        1440
      ],
      "frontFlight": [
        1320,
        1440
      ],
      "frontHit": 1440,
      "rearFlight": [
        1800,
        1920
      ],
      "rearHit": 1920
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
      "effect": "수평 창/충격 궤적이 몬스터 전면에서 플레이어 쪽으로 이동",
      "response": "dodge"
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
      "effect": "지면 가까운 수평 베기가 몬스터에서 플레이어 쪽으로 이동",
      "response": "dodge"
    },
    {
      "id": "high",
      "name": "상단 베기",
      "dodge": "down",
      "reviewLabel": "머리 높이의 띠 · 아래 회피로 쳐내기",
      "rect": [
        248,
        270,
        376,
        88
      ],
      "anchorRule": "상단 금빛 범위를 향해 제자리에서 검을 휘두른다. 파동이 중앙에 맞닿을 때 아래 회피 입력.",
      "effect": "상단 궤적과 플레이어 검이 맞부딪힌 뒤 위쪽으로 튕겨 나감",
      "response": "parry"
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
      "effect": "몬스터가 보낸 궤적이 위로 돌아 등 뒤와 현재 자리에 낙하",
      "response": "dodge"
    }
  ],
  "visibility": {
    "directionText": false,
    "answerArrow": false,
    "preHighlightedCorrectButton": false,
    "timingBar": false,
    "normalDodgeControls": true,
    "simultaneousFlashingAreas": 1,
    "reducedFlash": "constant gold fill0.12 during visible cue; three corner contractions; keep essential large circular fronts only",
    "crouchControl": false,
    "downParryControl": true
  },
  "geometryContract": "Rectangles are dangerous areas, never safe zones. Shared authored AttackShape and player dodge-body poses must drive both the visual and Core hit decision. No hidden required-direction failure for a dodge that actually leaves the shown footprint.",
  "previewScope": "Runtime-aligned v5.9.1. Four direction controls / WASD / arrows evade; Down uses a defensive sword swing for high attacks. Space attacks. Full HP/save/offense verified in Unity.",
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
    "sources": "Stored Unity ParticleSystem/TrailRenderer/LineRenderer pools; screen overlay for timing/perfect waves",
    "dangerVsDecoration": "danger rectangle is the full gameplay footprint; outlying sparks/dust/rings are decorative and do not deal damage",
    "palette": "enemy gold/amber; confirmed perfect mint/ivory",
    "audioPlan": "wind-up metal tension, release whoosh, impact low thump with metallic tick; not synthesized in this preview"
  },
  "timingAssist": {
    "origin": "two hidden fixed screen-space sources (-0.125W,0.5H) and (1.125W,0.5H)",
    "color": "#C7DAFA",
    "firstLeadMs": 600,
    "followupLead": "Every hit emits at T-2.5 beats and becomes visible at T-2 beats; identical speed throughout the sequence",
    "motion": "p=clamp((clock-start)/(T-start)); radius=(0.5+0.125)*W*p; same radius on both axes",
    "perfectTargetFromMs": -60,
    "perfectTargetUntilMs": 0,
    "target": "small fixed diamond; four short brackets appear during T-60..T; neutral contact pulse is never a success signal",
    "lateInputGraceMs": 0,
    "successWave": "existing confirmed-only mint circles expand outward",
    "drawLayer": "above perfect-result rings and world VFX, below controls; fixed screen coordinates",
    "reducedMotion": "keep one essential expanding circle per side; no halo, echo, particles or success expansion",
    "shape": "true circles expanding from radius zero; full-viewport clipping, no short translated curves",
    "sourceOffsetWidth": 0.125,
    "primaryStrokePx": 4,
    "haloStrokePx": 18,
    "echoRadiusGapPx": 28,
    "firstEmissionLeadMs": 600,
    "firstVisibleLeadMs": 480,
    "contact": "radius=0.625W; leftSourceX+radius=rightSourceX-radius=W/2 at T",
    "contactAfterglowMs": 45,
    "runtimeContactInput": "Unity displays the exact T contact frame, then collects its input before resolving; not an added post-T window."
  },
  "bells": {
    "centerNormalized": [
      0.5,
      0.3333333333333333
    ],
    "iconSize": [
      28,
      34
    ],
    "slotSpacingPx": 44,
    "maxCount": 3,
    "meaning": "visible bells = unresolved attacks; show whole count at cue start; remove left-to-right on each impact regardless of hit/dodge",
    "layout": "reserve initial slot positions; do not recenter remaining bells; no ghost bells",
    "clearOn": [
      "enemy death",
      "stun cancelling the queue",
      "combat end"
    ],
    "pause": "freeze with combat clock"
  },
  "movementTarget": {
    "source": "hitbox-preview-v55.js: dodge, heroState, drawHero",
    "backDodge": {
      "distancePx": -90,
      "enterMs": 50,
      "holdAfterImpactMs": 100,
      "returnMs": 130,
      "curve": "1-(1-p)^3",
      "pose": "hero-dodge-back/08.png"
    },
    "attack": {
      "homeX": 375,
      "strikeX": 540,
      "approachMs": 120,
      "hitMs": 200,
      "returnStartMs": 230,
      "endMs": 500,
      "curve": "1-(1-p)^3",
      "pose": "hero-v6-slash-up frames 0..10,17,23,24"
    },
    "status": "Applied to Unity in v5.9.1"
  },
  "hitboxExposure": {
    "defaultMode": "short",
    "options": {
      "short": {
        "defaultMs": 720,
        "fastMs": 720
      }
    },
    "blinkCount": 3,
    "schedule": "Three half-bright/half-dim periods. Final half beat is the attack flight, not extra waiting time.",
    "afterVisibleCue": "outline alpha .14 until impact"
  },
  "parry": {
    "input": "Down dodge button; S or DownArrow. Attack remains ordinary offense/counter.",
    "normalWindowMs": 160,
    "perfectWindowMs": 60,
    "sharedDefenseReuseMs": 260,
    "attackCooldownInteraction": "timed parry is available independently of500ms offensive cooldown; does not reset it",
    "interruptOffense": "before its contact cancel the pending damage; after contact preserve already dealt damage; original offensive cooldown remains",
    "priority": "Down => defensive sword swing; only a high threat inside the defense window is parried. No crouch.",
    "position": "snap to battle anchor and swing upward in place; no forward approach for a parry",
    "damage": 0,
    "normalStagger": 20,
    "perfectStagger": 40,
    "counterEligibilityMs": 500,
    "success": "neutralize only the selected high threat; keep later attacks unless enemy death/stun cancels them",
    "wrongInput": "jump/back/forward remain inside the high footprint; no crouch input; geometry remains authoritative",
    "feedback": "ivory crossed blades and reflected enemy trail; perfect only adds center mint wave + label",
    "scope": "Implemented in Unity; Canvas demonstrates defense timing and direction, not full HP/save/offensive cooldown rules."
  },
  "fastFlightMs": 80
};
