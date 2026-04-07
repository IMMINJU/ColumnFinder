# ColumnFinder

탐색기 형태에 컬럼뷰만 들어간 것이 필요해 직접 만들었습니다.

![ColumnFinder](app.ico)

## 어떻게 생겼나

- 사이드바 + 주소창 + 툴바 — 윈도우 탐색기 그대로
- 메인 영역만 컬럼뷰
- 파일을 클릭하면 마지막 컬럼이 미리보기로 바뀜 (맥 Finder랑 똑같이)
- 라이트 테마. 시스템 아이콘 그대로

## 기능

- 컬럼뷰 
- 사이드바 (홈 / 바탕화면 / 다운로드)
- 브레드크럼 주소창. 빈 영역 클릭하면 텍스트 편집모드
- 시스템 셸 아이콘
- 파일 작업 (잘라내기/복사/붙여넣기/이름변경/삭제는 휴지통)
- 드래그 앤 드롭 (외부 앱이랑도 됨)
- 정렬 (수정일/이름/크기/유형)
- 검색 (`Ctrl+F`)
- 숨김 파일 토글 (`Ctrl+H`)
- 컬럼 너비 드래그
- 컬럼 1개일 땐 영역 다 차지하고, 늘어나면 균등 분할
- 우클릭 메뉴 (자체 메뉴 + "탐색기에서 보기"로 7-Zip 같은 셸 확장 위임)

## 단축키

| 키 | 동작 |
|---|---|
| `←` `→` | 컬럼 간 이동 |
| `↑` `↓` | 컬럼 안에서 이동 |
| `Enter` | 파일 열기 / 폴더 진입 |
| `Ctrl+L` | 주소창 편집 |
| `Ctrl+F` | 검색 |
| `Ctrl+H` | 숨김 파일 |
| `Ctrl+C` `Ctrl+X` `Ctrl+V` | 복사 / 잘라내기 / 붙여넣기 |
| `F2` | 이름 바꾸기 |
| `Delete` | 휴지통 |

## 다운로드

[Releases 페이지](https://github.com/IMMINJU/ColumnFinder/releases)에서 최신 빌드를 받을 수 있습니다. 압축 풀어서 `ColumnFinder.exe`만 실행하면 끝.

## 직접 빌드하고 싶다면

.NET 8 SDK 필요.

```bash
dotnet build
dotnet run
```

단독 실행 파일:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out
```

## 기술 스택

- WPF + .NET 8
- Vanara.PInvoke.Shell32

## 라이선스

MIT
