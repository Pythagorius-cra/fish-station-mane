# Инвентарь SS220 Xenobiology

Источник поведения: `ss220club/Paradise-SS220`, ветка по умолчанию `master`, дерево `0fffca1f1f29a63aff49d9b56524813eedf82c6d` (репозиторий архивный, язык DM, родитель Paradise). Вики вторична и в этот список не подмешивалась.

## Шапка обнаружения

- Форк, из которого идёт PR: `Pythagorius-cra/fish-station-mane` (`origin`). Это не upstream.
- Upstream: `makura-games/fish-station`. Ветка по умолчанию по API GitHub: `master`. Tip на момент нарезки: `e98fb511c9ddf4d8e28d27110539a9fa48395c94`.
- Рабочая ветка: `feature/ss220-xenobiology` (имени на origin не было). Запрещённые ветки не читались и не пушились.
- Стиль PR Pythagorius-cra по живым PR в `makura-games/fish-station`: русский, разговорный, заголовок без корпоративного тона. `#584` — «Вернули музыкальный автомат, блять, и набили его треками» (`feature/restore-original-music-player`, draft). `#581` — «Расы бесплатно + пачка багфиксов с Discord» (`feature/all-races-free`). Секции в духе «что это», «что чинил», «что сознательно не трогал», чеклист проверки. Префикс `[Fish]` в этих двух заголовках не стоял; в задании он обязателен, поэтому заголовок PR будет `[Fish] ...`.
- `CONTRIBUTING.ru.md`: публично открыты YAML/спрайты/звуки/карты/переводы/доки. C# PR требует членства в команде или issue с меткой «Одобрено». Это ограничение репозитория, не повод выкидывать геймплей.

Префикс форка: `Fish`, папка `_Fish`, маркеры только `Fish-edit` и `Fish-start` / `Fish-end`. Новые прототипы: id с `Fish` и суффикс `Fish`.

## 1. Слайм: тело, рост, голод, деление

Оригинал: `code/modules/mob/living/simple_animal/slime/slime_mob.dm`, `slime_life.dm`, `slime_powers.dm`, `slime_death.dm`, `subtypes.dm`. Порог эволюции: `#define SLIME_EVOLUTION_THRESHOLD 10` в `code/__DEFINES/mob_defines.dm`.

Назначение: живой корм-цикл ксенобиологии. Серый малыш ест, растёт, становится взрослым, делится на четырёх детей с шансом мутации.

Состояние:

- Цвет, baby/adult, nutrition, amount_grown, mutation_chance (старт 30, у ребёнка clamp(parent ±5, 0, 100)), cores (старт 1, потолок 5), powerlevel 0–10, rabid, docile, Discipline, attacked, trained, mutator_used, Target.
- Малыш: HP 150, nutrition старт 700, max 1000, grow 800, hunger 500, starve 200.
- Взрослый: HP 200, max 1200, grow 1000, hunger 600, starve 300.
- Docile держит nutrition 700 и не охотится.
- Каждые life: 15% шанс сжечь `1 + adult` nutrition. На нуле 75% шанс урона. Выше порога роста: −20 nutrition и +1 growth, пока не 10.
- На 10 growth без цели, без кормления и без игрока: малыш эволюционирует, взрослый делится.
- Деление: 4 малыша. Если `mutation_chance >= 100` — rainbow. Иначе `prob(mutation_chance)` берёт один из четырёх слотов таблицы, иначе цвет родителя. Nutrition ребёнка игрока = 90% родителя. Powerlevel ребёнка = powerlevel/4. Игрок переносится в случайного ребёнка, родитель удаляется.
- Смерть взрослого (не gib): спавн бешеного малыша того же цвета, сам откатывается в малыша и оживает. Малыш умирает. Gib удаляет.
- Огонь лечит brute (снаряды burn тоже). Вода: `rand(10,15) * volume` brute; огнетушитель злит обученного. Холод ниже 5°C копит урон, ниже −40°C стан. Жара ≥ 57°C ускоряет.
- Морфин ×2 к задержке движения, frostoil ×5. EMP обнуляет powerlevel.
- Кормление: присосаться к живому не-слайму не-силикону. Carbon: clone 2–4 и tox 1–2 за тик, слайм +7–15 nutrition и −3 brute. Один слайм на жертву. Смерть жертвы-игрока-человека: 85% rabid. Драться с присосавшимся: шанс сорвать и дисциплинировать.
- Плазма: 5 листов. Если уже есть Discipline и не trained — trained, имя «tamed». Иначе позитивная дисциплина.
- Речь/настроение по голоду, rabid, trained, температуре. Венткравл, не тянет предметы.

SS14: `FishSlimeComponent` + система на таймере 2 с, не каждый кадр. Урон через `DamageableSystem`, голод — поле компонента, а не кухня. ИИ — выбор цели в радиусе 7 и шаг к ней. Игрок на слайме не автоделится: глагол.

Проверка: юнит на таблицу мутаций и порог 10; сборка сервера.

## 2. Таблица мутаций

Оригинал: `mutation_table()` в `subtypes.dm`. Слот выбирается `rand(1,4)`, то есть веса не равны, если цвет повторён.

| Цвет | Слоты |
| --- | --- |
| grey | orange, metal, blue, purple |
| purple | dark purple, dark blue, green, green |
| metal | silver, yellow, gold, gold |
| orange | dark purple, yellow, red, red |
| blue | dark blue, silver, pink, pink |
| dark blue | purple, blue, cerulean, cerulean |
| dark purple | purple, orange, sepia, sepia |
| yellow | metal, orange, bluespace, bluespace |
| silver | metal, blue, pyrite, pyrite |
| pink | pink, pink, light pink, light pink |
| red | red, red, oil, oil |
| gold | gold, gold, adamantine, adamantine |
| green | green, green, black, black |
| остальные (light pink, oil, black, adamantine, bluespace, cerulean, sepia, pyrite, rainbow) | четыре раза свой цвет |

`random_colour()` / rainbow-плазма берёт равномерно из списка цветов в `slime_mob.dm` (22 имени, включая rainbow).

Поля `effectmod` и `applied` объявлены, потребителей в скачанных файлах слайма и ксенобиологии нет. `#define SLIME_EXTRACT_CROSSING_REQUIRED 10` есть, логики скрещивания ядер в этих файлах нет. Скрещивание не выдумывается.

## 3. Экстракт

Оригинал: `/obj/item/slime_extract` в `xenobiology.dm`. 1 использование, реагенты 100, инъекция шприцом запоминает инжектора. Enhancer: +1 использование, потолок 5. Цвета и `associated_gun_type` — отдельная строка на цвет (оружие ядра в реакциях экстрактов не спавнится, это поле предмета).

Процессор: `code/modules/food_and_drinks/kitchen_machinery/processor.dm`, рецепт `food_processor_process/mob/slime`. Живой слайм вылезает. Мёртвый даёт `cores + rating - 1` экстрактов своего `coretype`.

SS14: отдельная машина `FishSlimeProcessor`, мёртвый слайм, питание, выход `cores` экстрактов (рейтинг деталей не портируем, пока нет stock parts на этой машине — в отчёте). Шприц и `InjectableSolution` — штатные.

## 4. Реакции экстрактов

Файл: `code/modules/reagents/chemistry/recipes/slime_extracts.dm`. Контейнер — экстракт. Реагент тратится, использование снимается. Объёмы как в DM.

| Реакция | Цвет | Реагент | Продукт |
| --- | --- | --- | --- |
| Slime Spawn | grey | plasma 1 | серый малыш |
| Epinephrine | grey | water 5 | epinephrine 3 |
| Monkey | grey | blood 1 | 3 monkey cube |
| Mutation toxin | green | plasma 1 | mutationtoxin 1 |
| Metal | metal | plasma 1 | plasteel 5 + metal 15 |
| Glass | metal | water 1 | rglass 5 + glass 15 |
| Smart metal | metal | blood 1 | synthetic omnizine 1 |
| Crit | gold | plasma 1 | 5 враждебных мобов, задержка, общий кап |
| Crit lesser | gold | blood 1 | 3 нейтральных |
| Crit friendly | gold | water 1 | 1 дружелюбный |
| Bork | silver | plasma 1 | 4–6 случайной еды |
| Bork 2 | silver | water 1 | 4–6 случайных напитков |
| Frost oil | blue | plasma 1 | frostoil 10 |
| Stabilizer potion | blue | blood 1 | зелье стабилизатора |
| Freeze | dark blue | plasma 1 | через ~5 с −240 температуры в радиусе 7 |
| Fireproof potion | dark blue | water 1 | chill potion, 3 заряда |
| Capsaicin | orange | blood 1 | capsaicin 10 |
| Fire | orange | plasma 1 | через ~5 с горячий плазменный выброс |
| EMP | yellow | blood 1 | EMP 3/7 |
| Powercell | yellow | plasma 1 | high slime cell |
| Glow | yellow | water 1 | slime flashlight |
| Steroid | purple | plasma 1 | стероид |
| Jam | purple | sugar 1 | slimejelly 10 |
| Plasma sheets | dark purple | plasma 1 | plasma sheet 3 |
| Mutator | red | plasma 1 | мутатор |
| Bloodlust | red | blood 1 | видимые слаймы: docile снимается, иначе rabid |
| Speed | red | water 1 | speed treat |
| Docility | pink | plasma 1 | docility potion |
| Omnizine | pink | blood 1 | omnizine 1 |
| Advanced mutation | black | plasma 1 | amutationtoxin 1 |
| Virus genes | black | blood 1 | virus_genes 1, бутылка, штамм если был вирус |
| Explosion | oil | plasma 1 | через 5 с взрыв devastation 1 / heavy 3 / light 6, лог инжектора |
| Oil potion | oil | blood 1 | oil slick potion |
| Sentience | light pink | plasma 1 | sentience potion |
| Adamantine | adamantine | plasma 1 | лист адамантина |
| Bluespace floor | bluespace | blood 1 | 25 блюспейс-плиток |
| Crystal | bluespace | plasma 1 | bluespace crystal |
| Enhancer | cerulean | plasma 1 | extract enhancer |
| Territory | cerulean | blood 1 | cerulean blueprints, одноразовые, помечают зону |
| Timestop | sepia | plasma 1 | хронополе 140 тиков, радиус 2, автор иммунен |
| Camera | sepia | water 1 | камера + плёнка |
| Sepia floor | sepia | blood 1 | 25 сепия-плиток |
| Paint | pyrite | plasma 1 | случайная банка краски |
| Random core | rainbow | plasma 1 | случайный слайм, 50% взрослый |
| Transfer | rainbow | blood 1 | consciousness transference potion |

У gold/silver/purple нет третьей реакции в этом файле (у части цветов две, не три). Число `MAX_GOLD_CORE_MOBS` в скачанных файлах не попалось; кап в порте фиксируется в коде и пишется в паритет.

## 5. Зелья

Оригинал: `xenobiology.dm` от `/obj/item/slimepotion`.

- Stabilizer: mutation_chance −15, не ниже 0. На мёртвого и не-слайма не работает.
- Mutator: +12, один раз на слайма, не если уже 100.
- Steroid: только малыш, cores +1, потолок 5.
- Enhancer: на экстракт, uses +1, потолок 5.
- Docility: снимает rabid без полной docile; иначе docile, nutrition 700, имя от игрока.
- Speed: bodytemperature 550, джиттер 15 с, не стакается по смыслу «уже горячий». Слайм может съесть сам.
- Fireproof: одежда, 3 заряда, имя fireproofed, цвет, FIRE_PROOF и максимальная теплозащита.
- Oil slick: slowdown предмета в 0, либо стол slippery. Цвет и трейт OIL_SLICKED. Активный MOD нельзя.
- Sentience: опрос призраков 10 с, причина обязательна. Уже с разумом, мёртвые и неорганика — отказ. Два холостых применения греют зелье, третье взрывается. Остывание −1 стадия за 60 с.
- Transference: игрок подтверждает, разум уходит в животное, старое тело умирает. Роли сохраняются.

Отдельных зелий «acidproof» и «explosion resistance» в этих файлах нет. Кислотостойкость и взрывостойкость как зелья не выдумываются. Взрыв — реакция oil, огнезащита — chill potion.

## 6. Консоль управления слаймами

Оригинал: `code/modules/research/xenobiology/xenobio_camera.dm`, глаз `code/modules/mob/camera/eye/xenobio_eye.dm`.

Команды: положить всех хранимых слаймов на тайл глаза; подобрать слаймов с тайла (лимит 6, с ckey нельзя, кормление срывается); выпустить обезьяну из запаса кубов (тип куба с ресайклера); переработать мёртвую мелкую обезьяну в доли куба `cube_production / required_grind`; скан; скормить загруженное зелье. Хоткеи: shift слайм — забрать, shift пол — выпустить всех, ctrl слайм — скан, alt слайм — зелье, ctrl обезьяна — ресайкл, ctrl пол — обезьяна. Зона: та же area, что у глаза, или `xenobiology_compatible`, и видимость камеры. В консоль вставляют кубы (и из сумок), зелье слайма (замена предыдущего). Мультитул линкует ресайклер. При уничтожении слаймы выпадают.

Скан показывает питание, рост, мутацию, ядра, силу, состояние. Точный текст `slime_scan()` в скачанных файлах не найден (вызов есть, определения нет) — порт печатает те же поля, которые консоль и examine реально используют.

SS14: камерного глаза ксенобиологии в билде нет. Команды консоли работают по слаймам и обезьянам в радиусе лаборатории у консоли, с проверкой доступа Research и питания. Это сохраняет цикл, не камеру станции. Дыра пишется в паритет.

## 7. Ресайклер обезьян

`code/modules/food_and_drinks/kitchen_machinery/monkeyrecycler.dm`.

Мёртвая мелкая обезьяна, не живая. `grinded++`. Выдача, когда `grinded >= required_grind` (старт 5, минус рейтинг манипулятора). Кубов за раз = рейтинг matter bin (старт 1). Мультитул по закрытой панели крутит тип куба: monkey, nian worme, farwa, wolpin, stok, neaera, обратно monkey. По открытой панели — буфер для линка. Питание, якорь, отвёртка/лом/ключ.

SS14: мёртвый `MobMonkey` (есть `MonkeyAccentComponent`). Типы кубов, которых нет в билде, не подменяются молча: крутится только существующий `MonkeyCube`. Рейтинг деталей не крутится, цифры как у стоковых частей (5 туш → 1 куб).

## 8. Обезьяньи кубы

В SS14 уже есть `MonkeyCube` / `Rehydratable` от воды. Серый экстракт спавнит три куба. Консоль хранит число, не предметы. Ресайклер выдаёт куб. Кормление слайма — по живой обезьяне.

## 9. Сканер

Отдельного предмета «slime scanner» в дереве файлов нет (есть перевод `slime_scanner.toml` и кнопка консоли). Порт: ручной сканер с теми же полями, что консоль, чтобы измерение не жило только внутри UI.

## 10. Органы, диссекция, анализатор

Оригинал:

- `xenobiology_dissections.dm`: инструмент dissector, шаг хирургии dissect, качество органа, контейнеры добычи по типам мобов (goliath, flock, megacarp, morph, hound, watcher, spider, hivelord, goldgrub, tomato, herald, clown, revenant, colossus, pandora, legionnaire, bubblegum и другие).
- `xenobiology_organs.dm`: неопознанная масса `xeno_organ` с `true_organ_type` и `unknown_quality`. Обученный взрослый слайм без разума втягивает орган, 50 тиков `handle_organs`, выплёвывает готовый орган с качеством. Десятки органов с эффектами (печень-токсин, огненные лёгкие, sinew, гиперсердце, жало, и дальше по файлу ~1600 строк).
- `organ_analyzer.dm`: только уже оживлённый ксено-орган, не больше 3 сканов типа, 15 с, награда в научный счёт станции от цены и качества (pristine ×2, damaged ×0.5) и рейтинга сканеров. Может выдать tech disk.
- `regen_mesh`: поднимает качество ксено-органа до pristine.

SS14: полный список уникальных заклинаний органов (дыхание дракона, flock, revenant) не имеет прямых датамов. Порт делает рабочий конвейер: диссектор по мёртвому мобу из таблицы → неопознанная масса → обученный слайм → готовый орган с эффектом, который ложится на существующие системы (урон, скорость, реагенты), анализатор начисляет очки ближайшему research server. Органы без честного эффекта не помечаются как сделанные.

## 11. Лаборатория

Игроки на станции заказывают ящик науки: консоль, процессор, ресайклер, сканер, анализатор, диссектор, серый малыш, кубы, плазма, шприц, кровь. Машины питаются от APC. Доступ Research. Новую карту в `SunriseMapPool` не добавляем: в пуле прямо написано не добавлять карты без ревью. Существующие гриды не переписываются.

## 12. Спрайты

`icons/mob/slimes.dmi` SS220 не копируется вслепую. В билде уже есть `Mobs/Aliens/slimes.rsi` (CC-BY-SA, tg): blue/green/yellow adult, rainbow baby и rainbow extract. Остальные цвета — тот же спрайт с цветовым множителем, разным по цвету. Отдельных недостающих state в прототипах нет.

## Проверка

Юнит на таблицу и порог. `dotnet build` затронутых проектов. YAML linter, если прототипы собраны. В паритете только то, что реально запускалось.
