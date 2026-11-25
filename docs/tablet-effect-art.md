# 태블릿 퍼즐 효과 리소스

태블릿 모드의 보드, 하단 팔레트, 행동 결과와 체력·방어막 HUD에서 같은 아이콘을 사용합니다. ImageGen 내장 도구에서 만든 하나의 RGBA 원본을 칸별로 확인한 뒤 실제 알파를 보존하며 208×208 투명 캔버스 중앙에 맞췄습니다. Unity에는 각각 중앙 피벗의 Sprite로 가져옵니다.

| 색 ID | 표시 | 파일 |
| ---: | --- | --- |
| 1 | 빨강 직선탄 | `effect-straight.png` |
| 2 | 주황 관통탄 | `effect-piercing.png` |
| 3 | 노랑 확산탄 | `effect-spread.png` |
| 4 | 초록 수리 | `effect-repair.png` |
| 5 | 하늘 유도탄 | `effect-homing.png` |
| 6 | 파랑 방어막 | `effect-shield.png` |
| 7 | 보라 폭발탄 | `effect-blast.png` |

리소스 경로는 `Rainbow/Assets/Resources/Tablet/`입니다. 원본 atlas는 1774×887 RGBA, SHA-256은 `fda5b335d7661a461b71300f3e7bb7838b868032cd1bf676df7006178f140bbb`입니다. 첫째 줄 네 칸과 둘째 줄 세 칸을 왼쪽부터 선택했고 마지막 빈 칸은 배제했습니다. 이 프로젝트용으로 생성한 이미지의 적용 약관은 다음과 같습니다. [OpenAI 이용약관의 Output 조항](https://openai.com/policies/terms-of-use/#content)

## 생성 프롬프트

```text
Generate one original game UI icon sprite sheet as a PNG with genuinely transparent alpha. EXACTLY 4 columns by 2 rows, centered in each equal-size rectangular grid cell. The eight centers are at x=12.5%,37.5%,62.5%,87.5% with row centers at 25% and 75% of the canvas. Wide 2:1 sprite sheet; ample transparent gutters, no artwork beyond each cell, and the last cell must be entirely blank. Exactly seven glyphs, ordered: TOP ROW (1) single straight ivory energy bullet pointing UP, one distinct simple needle silhouette, (2) one thick amber piercing projectile passing cleanly through one circular metal ring/plate, visible ring intersection, (3) exactly three golden bolts spread in an open fan from one source, (4) one mint-green rounded heart with a bold white medical PLUS at its middle. SECOND ROW (5) one cyan guided missile with a conspicuous single sweeping curved flight arrow leading toward small target dot, (6) one solid cobalt fortress defense shield with simple white glint corner and clear broad silhouette, (7) one violet stylized energy explosion eight-pointed blast with central white flare and two tiny particles close to it, (8) blank. Icon language: approachable emoji-style videogame pictograms, intentionally very simple asymmetric silhouettes that differ by shape, bright near-white hot elements for a dark colored square behind each, outline in dark deep navy visibly 4–5% of the icon width, soft painted value shading. Each fits a compact 48-pixel mobile tile. No tile or label backgrounds, no circle container except piercing ring and homing path, no text, no numbers, no checkers, no people, no faces, no drop shadows beyond each transparent object's edge. Render large inside each cell with about 15% breathing room.
```
