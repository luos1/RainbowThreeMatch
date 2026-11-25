# 태블릿 모함 리소스

내장 GPT Image에서 모함 PNG 한 장을 생성했습니다. Unity에서는 하단 마스크로 선수만 노출하는 원본 그대로 사용합니다. 1254×1254 RGBA이며 상단 배경과 선체 윤곽에 실제 투명 알파가 있습니다. 포문은 이미지의 가로 24%, 50%, 76% 부근에 있고, 게임의 고정된 발사 위치는 노출된 세 포문 끝에 대응합니다. 화면 밖의 나머지 선체는 피격 범위에 포함하지 않습니다.

- 선택 파일 `Rainbow/Assets/Resources/Tablet/carrier-bow.png`
- SHA-256 `57618186fbc3ad74e2191af651ecba5985ffa73a0d5dbc4ea3da766c704ae157`
- 내장 도구에서 이 프로젝트용으로 생성했습니다. [OpenAI 이용약관의 Output 조항](https://openai.com/policies/terms-of-use/#content)

## 생성 프롬프트

```text
Create a SINGLE production-ready top-down 2D PNG game sprite on genuinely transparent alpha, one original massive sci-fi aircraft-carrier-like command ship pointing straight UP (north), orthographic exactly overhead, spacecraft flat lay, no vanishing point. Important composition: the playable camera only shows the TOP 28-36 percent of this sprite from its front-down; the remaining huge main hull continues below the game screen. Make the complete silhouette axis-symmetric and appreciably wide (about 86% of canvas width), with a broad blunt armored forward bow, not a thin aircraft wedge. The foremost bow occupies upper center and is wide at the front with roughly a flat straight shoulder line, narrowing very slightly at the very tip. Put a very clear central forward cannon at the north nose and two forward-facing dual-cannon emplacements near the front-left and front-right shoulders, all within the top quarter of the ship. The first 30% must read as part of a huge ship with layered dark slate titanium armor, warm ivory outer armor panels, thin amber brass edging, small teal power grooves, and high-contrast crisp contours that remain readable at 160 pixels. The broad side plates should continue linearly downward into the bulk under the camera; dense engraved plating and orderly panel seams without noise. Crisp stylized retro arcade and painted shaded metal, premium sci-fi game sprite. Square canvas with minimal transparent padding, do not crop the actual sprite. No starfield, no floor, no shadow cast onto background, no halo, no writing, no logo, no engines exhaust or rockets, no accompanying ships, no humans, no image checkerboard.
```
