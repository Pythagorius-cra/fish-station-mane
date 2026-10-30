# Прогресс порта ксенобиологии

## Слаймы (секции 1–2)

Последний SHA: `01f4d80af73f5e9617d75e5b462cd81562b6e6f6`.

Есть: 22 цвета, малыш/взрослый, голод и пороги роста, деление на 4 по таблице SS220, смерть взрослого (малыш + бешеный малыш), присасывание, плазма (5 листов), вода, огонь лечит, холод замедляет, EMP сбрасывает заряд.

Нет в этом куске: экстракты, зелья как предметы, консоль, ресайклер, лаборатория. Поля `cores` / `docile` / `trained` на компоненте есть, предметы зелий не мои.

Сборка: `dotnet build Content.Server/Content.Server.csproj --configuration Debug --verbosity quiet --no-restore` — succeeded.

Юнит: `dotnet test Content.Tests/Content.Tests.csproj --configuration Debug --filter FullyQualifiedName~FishSlimeMutationTest` — Passed 7, Failed 0. YAML-линтер не запускался. В игре не проверялось.
