window.HITBOX_PREVIEW_SPEC = {
  "version": "hitbox-preview-v5.3",
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
  "impactEffectMs": 100,
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
  "gaugeBounds": [
    303,
    318,
    144,
    16
  ],
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
  "timingRule": "cueMs=impactMs-flightMs; blinkPeriod=cueMs/3; first half bright, second half dim; launch exactly after third dim; impact at T",
  "combo": {
    "nextCueStartsAfterImpactMs": 150,
    "minTelegraphMs": 650,
    "minImpactGapMs": 800,
    "defaultImpactGapMs": 1050,
    "nextPreview": "none until current hit resolves"
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
    "timingBar": true,
    "normalDodgeControls": true,
    "simultaneousFlashingAreas": 1,
    "reducedFlash": "same footprint, constant fill alpha0.12; outline contracts three times; timing bar unchanged"
  },
  "geometryContract": "Rectangles are dangerous areas, never safe zones. Shared authored AttackShape and player dodge-body poses must drive both the visual and Core hit decision. No hidden required-direction failure for a dodge that actually leaves the shown footprint.",
  "previewScope": "Existing static/selected poses plus schematic attack effects; no new enemy animation or Unity runtime implementation. All times/bounds are proposal values."
};
