# Unity-Bootcamp
# 김연주 | Unity Client Portfolio Hub

Unity 기반 게임 클라이언트 개발 포트폴리오입니다.
두 개의 팀 프로젝트를 통해 월드 시스템, 런타임 최적화, UI/UX 상태 관리, 성장 시스템, 타이틀 연출을 구현했습니다.

---

## 1. Project NOVA

**장르**: 2D 방치형 우주 탐사 RPG
**역할**: 팀 리드 / UI·성장 시스템 / 타이틀 연출
**핵심 키워드**: Equipment System, Crew System, Planet UI, Title Transition, UI/UX Feedback

### 핵심 구현

* **Equipment System**

  * 장비 제작, 강화, 티어업, 해금 조건 관리
  * 장비 상태에 따라 잠김 / 미제작 / 강화 가능 / 최고 레벨 / 티어 상승 가능 UI 분기

* **Crew System**

  * 엔지니어, 탐사자, 보안요원 성장 및 해금 구조 구현
  * 레벨업, 등급 업그레이드, 해금 VFX, 선택 피드백 UI 적용

* **Explore / Planet UI**

  * 계층과 행성 해금 조건을 UI에 반영
  * 탐사 전 승무원·장비 상태를 요약해 보여주는 UI 흐름 구성

* **Title Transition**

  * 시작 버튼 클릭 후 암전, 문 오픈, 웜홀, 워프, 플래시, 로그인 팝업으로 이어지는 타이틀 연출 구현
  * 스킵 버튼, BGM Fade Out, SFX 동기화 처리

### 대표 코드

* `EquipmentManager.cs`
* `EquipmentUI.cs`
* `EquipmentDetailUI.cs`
* `CrewManager.cs`
* `CrewUI.cs`
* `PlanetListUI.cs`
* `TitleTransitionController.cs`

### 시연 자료

* Project NOVA 시연 영상: https://youtu.be/1-ljXu4yQG0
* UI Flow 이미지: docs/project-nova
* [기술 문서](docs/project-nova/기술문서.md)

---

## 2. Coin Survivor

**장르**: 2D 생존 액션 / Vampire Survivors-like
**역할**: 월드 시스템 / 런타임 최적화 / Enemy 관리
**핵심 키워드**: Chunk Streaming, Object Pooling, Enemy Runtime, SpatialGrid, GC Optimization

### 핵심 구현

* **Chunk 기반 맵 스트리밍**

  * 플레이어 위치 기준으로 필요한 Chunk만 활성화
  * 멀어진 Chunk는 Object Pool로 반환하여 맵이 끊기지 않도록 구성

* **Enemy Runtime 관리**

  * EnemyFactory, EnemyRuntimeManager, ObjectPoolManager 구조 구현
  * 적 생성, 등록, 거리 기반 반환, 중복 등록 방지 처리

* **SpatialGrid 기반 최적화**

  * 모든 Enemy를 순회하지 않고 주변 Cell만 탐색하도록 개선
  * Separation Steering과 결합해 Enemy 겹침 완화

* **GC Alloc 최적화**

  * Unity Profiler로 GC Alloc 발생 지점 확인
  * 컬렉션 재사용, HashSet 중복 검사, SetTiles 배치 처리로 런타임 비용 감소

### 대표 코드

* `MapChunkManager.cs`
* `MapChunkPool.cs`
* `ObjectPoolManager.cs`
* `EnemyFactory.cs`
* `EnemyRuntimeManager.cs`
* `SpatialGrid.cs`
* `DroppedItemManager.cs`

### 개발 기록

* Issue #64: 전체 성능 최적화 요약 (https://github.com/LoveisChicken/Unity-Bootcamp/issues/64)
* Issue #60: MapChunk 빈 화면, PPU, GC 문제 (https://github.com/LoveisChicken/Unity-Bootcamp/issues/60)
* Issue #62: SetTile → SetTiles 개선 (https://github.com/LoveisChicken/Unity-Bootcamp/issues/62)
* Issue #65: SpatialGrid GC 최적화 (https://github.com/LoveisChicken/Unity-Bootcamp/issues/65)

### 시연 자료

* Coin Survivor 포트폴리오 영상: https://youtu.be/x93I-jS62pk
* 기술 문서: 끊김 없는 Chunk World와 자연스러운 Enemy 군집 이동 구현

---

## Contact

* GitHub: https://github.com/LoveisChicken
* Email: [younju7755@gmail.com](mailto:younju7755@gmail.com)
