using System;
using System.Collections.Generic;
using System.Globalization;
using MotorCity.Input;

namespace MotorCity.Localization
{
    public static class MotorCityLocalization
    {
        private const string Russian = "ru";
        private const string English = "en";

        private static readonly Dictionary<string, LocalizedEntry> Entries =
            new()
            {
                { "common.credits", E("{0:N0} КР", "{0:N0} CR") },
                { "common.rep", E("РЕП", "REP") },
                { "common.level", E("УР.", "LVL") },
                { "common.xp", E("ОПЫТ", "XP") },
                { "common.kmh", E("КМ/Ч", "KM/H") },
                { "common.gold", E("ЗОЛОТО", "GOLD") },
                { "common.silver", E("СЕРЕБРО", "SILVER") },
                { "common.bronze", E("БРОНЗА", "BRONZE") },
                { "common.finish", E("ФИНИШ", "FINISH") },
                { "common.no_medal", E("БЕЗ МЕДАЛИ", "NO MEDAL") },
                { "common.ready", E("ГОТОВО", "READY") },
                { "common.cancel", E("ОТМЕНА", "CANCEL") },
                { "common.new_record", E("НОВЫЙ РЕКОРД", "NEW RECORD") },
                { "common.record", E("Рекорд: {0:0.0}с", "Record: {0:0.0}s") },
                { "common.next", E("СЛЕДУЮЩИЙ", "NEXT") },
                { "common.available", E("ДОСТУПНА", "AVAILABLE") },
                { "common.max", E("МАКСИМУМ", "MAX") },
                { "common.bought", E("КУПЛЕНО", "OWNED") },
                { "common.start", E("НАЧАТЬ", "START") },
                { "common.point", E("ТОЧКА", "POINT") },
                { "common.night", E("НОЧЬ", "NIGHT") },

                { "hud.rep", E("РЕП {0:N0}   •   УР. {1}", "REP {0:N0}   •   LVL {1}") },
                { "hud.upgrades", E("ДВИГ {0}   •   СЦЕП {1}   •   СТАБ {2}   •   {3}", "ENGINE {0}   •   GRIP {1}   •   STAB {2}   •   {3}") },
                { "hud.drive_mode", E("РЕЖИМ  {0}", "MODE  {0}") },
                { "hud.drift", E("ДРИФТ   {0:N0}{1}", "DRIFT   {0:N0}{1}") },
                { "hud.drift_done", E("ДРИФТ ЗАВЕРШЁН   +{0:N0} КР", "DRIFT COMPLETE   +{0:N0} CR") },
                { "hud.secret_meeting", E("ТАЙНАЯ ВСТРЕЧА", "SECRET MEET") },
                { "hud.underground", E("НОЧНОЙ АВТОКЛУБ", "NIGHT CAR CLUB") },

                { "drive.comfort.name", E("КОМФОРТ", "COMFORT") },
                { "drive.sport.name", E("СПОРТ", "SPORT") },
                { "drive.drift.name", E("ДРИФТ", "DRIFT") },
                { "drive.comfort.desc", E("стабильная повседневная езда", "stable everyday driving") },
                { "drive.sport.desc", E("максимальная тяга и сцепление", "maximum grip and response") },
                { "drive.drift.desc", E("острый руль и свободная задняя ось", "sharp steering and freer rear axle") },

                { "vehicle.street.name", E("УЛИЧНАЯ", "STREET") },
                { "vehicle.club.name", E("КЛУБНАЯ", "CLUB") },
                { "vehicle.muscle.name", E("МАСЛКАР", "MUSCLE") },
                { "vehicle.gt.name", E("ГРАН-ТУРИЗМО", "GT") },
                { "vehicle.apex.name", E("АПЕКС", "APEX") },
                { "vehicle.street.desc", E("СБАЛАНСИРОВАННАЯ — универсальная городская машина", "BALANCED — versatile city car") },
                { "vehicle.club.desc", E("ЛЁГКАЯ — резкий руль и удобство в городе", "LIGHT — agile steering for city driving") },
                { "vehicle.muscle.desc", E("СИЛОВАЯ — мощный разгон и естественный дрифт", "POWERFUL — strong acceleration and natural drift") },
                { "vehicle.gt.desc", E("ТРЕКОВАЯ — тормоза, скорость и устойчивость", "TRACK — braking, speed and stability") },
                { "vehicle.apex.desc", E("ЭЛИТНАЯ — максимум темпа и точности", "ELITE — maximum pace and precision") },
                { "vehicle.first", E("Это первая машина в гараже", "This is the first car in the garage") },
                { "vehicle.last", E("Это последняя машина в гараже", "This is the last car in the garage") },
                { "vehicle.visual_missing", E("{0}: модель ещё не подготовлена", "{0}: vehicle model is not ready yet") },
                { "vehicle.rep_required", E("{0}: нужно {1:N0} РЕП", "{0}: requires {1:N0} REP") },
                { "vehicle.selected", E("Выбрана машина {0}", "Selected {0}") },
                { "vehicle.invalid", E("Некорректная машина", "Invalid vehicle") },
                { "vehicle.next_missing", E("   •   ДАЛЬШЕ: {0} — модель не подготовлена", "   •   NEXT: {0} — model unavailable") },
                { "vehicle.next_available", E("   •   ДАЛЬШЕ: {0} — КУПЛЕНА", "   •   NEXT: {0} — OWNED") },
                { "vehicle.next_buy", E("   •   ДАЛЬШЕ: {0} — КУПИТЬ ЗА {1:N0} КР", "   •   NEXT: {0} — BUY FOR {1:N0} CR") },
                { "vehicle.next_rep", E("   •   ДАЛЬШЕ: {0} — НУЖНО {1:N0} РЕП", "   •   NEXT: {0} — NEED {1:N0} REP") },
                { "vehicle.not_owned", E("{0}: сначала купи машину за {1:N0} КР", "{0}: buy this car first for {1:N0} CR") },
                { "vehicle.buy_none", E("Следующей машины для покупки нет", "There is no next car to buy") },
                { "vehicle.buy_rep", E("{0}: нужно {1:N0} РЕП, чтобы открыть покупку", "{0}: need {1:N0} REP to unlock purchase") },
                { "vehicle.buy_credits", E("Для покупки «{0}» нужно {1:N0} КР", "You need {1:N0} CR to buy “{0}”") },
                { "vehicle.purchased", E("Куплена машина {0} за {1:N0} КР", "Bought {0} for {1:N0} CR") },
                { "vehicle.garage_line", E("МАШИНА {0}/{1}: {2}   •   МАСТЕРСТВО {3}/10{4}", "CAR {0}/{1}: {2}   •   MASTERY {3}/10{4}") },
                { "vehicle.mastery_max", E("МАСТЕРСТВО: УР. 10/10   •   {0:N0} ОПЫТ   •   МАКСИМУМ", "MASTERY: LVL 10/10   •   {0:N0} XP   •   MAX") },
                { "vehicle.mastery", E("МАСТЕРСТВО: УР. {0}/10   •   {1:N0}/{2:N0} ОПЫТ", "MASTERY: LVL {0}/10   •   {1:N0}/{2:N0} XP") },
                { "vehicle.mastery_short", E("МАСТ {0}/10", "MAST {0}/10") },
                { "vehicle.stats", E("БАЗА: СКОРОСТЬ {0}   •   РАЗГОН {1}   •   СЦЕП {2}%   •   СТАБ {3}   •   {4}", "BASE: SPEED {0}   •   ACCEL {1}   •   GRIP {2}%   •   STAB {3}   •   {4}") },
                { "history.passport_title", E("ПАСПОРТ МАШИНЫ • {0}", "VEHICLE PASSPORT • {0}") },
                { "history.passport_summary", E("ПРОБЕГ {0:0.0} КМ   •   ПОБЕДЫ {1}   •   ЗАРАБОТАНО {2:N0} КР   •   СТАТУС {3}", "DISTANCE {0:0.0} KM   •   WINS {1}   •   EARNED {2:N0} CR   •   STATUS {3}") },
                { "history.passport_disciplines", E("ГОНКИ {0}   •   ДРИФТ {1}   •   ДОСТАВКИ {2}   •   ЛЮБИМОЕ: {3}", "RACING {0}   •   DRIFT {1}   •   DELIVERIES {2}   •   FAVORITE: {3}") },
                { "hud.passport_control", E("K ПАСПОРТ", "K PASSPORT") },

                { "garage.marker", E("Фиолетовый маркер: гараж", "Purple marker: garage") },
                { "garage.opened", E("ГАРАЖ ОТКРЫТ", "GARAGE OPEN") },
                { "garage.prompt", E("ГАРАЖ — нажми E", "GARAGE — press E") },
                { "garage.vehicles_unavailable", E("МАШИНЫ НЕДОСТУПНЫ", "VEHICLES UNAVAILABLE") },
                { "garage.max_level", E("МАКС", "MAX") },
                { "garage.level", E("УР. {0}/{1}", "LVL {0}/{1}") },
                { "garage.need_credits", E("Для «{0}» нужно {1:N0} КР", "“{0}” needs {1:N0} CR") },
                { "garage.already_max", E("{0} уже улучшен до максимума", "{0} is already maxed") },
                { "garage.upgraded", E("{0} улучшен до уровня {1}", "{0} upgraded to level {1}") },
                { "customization.color_line", E("ПОКРАСКА • {0}", "PAINT • {0}") },
                { "customization.color_red", E("КРАСНЫЙ", "RED") },
                { "customization.color_blue", E("СИНИЙ", "BLUE") },
                { "customization.color_yellow", E("ЖЁЛТЫЙ", "YELLOW") },
                { "customization.color_green", E("ЗЕЛЁНЫЙ", "GREEN") },
                { "customization.color_purple", E("ФИОЛЕТОВЫЙ", "PURPLE") },
                { "customization.color_white", E("БЕЛЫЙ", "WHITE") },
                { "customization.color_black", E("ЧЁРНЫЙ", "BLACK") },
                { "customization.summary", E("ВНЕШНИЙ ВИД • ЦВЕТ {0} • НАКЛЕЙКА {1} • ВИНИЛ {2} • ДИСКИ {3} • НЕОН {4} • НОМЕР {5}", "CUSTOM • COLOR {0} • STICKER {1} • VINYL {2} • WHEELS {3} • NEON {4} • PLATE {5}") },
                { "customization.simple_summary", E("ВНЕШНИЙ ВИД • ЦВЕТ {0} • ДИСКИ {1} • НЕОН {2}", "CUSTOM • COLOR {0} • WHEELS {1} • NEON {2}") },
                { "customization.sticker.0", E("НЕТ", "NONE") },
                { "customization.sticker.1", E("МОЛНИЯ", "BOLT") },
                { "customization.sticker.2", E("КРЫЛО", "WING") },
                { "customization.sticker.3", E("ТУРБО", "TURBO") },
                { "customization.vinyl.0", E("НЕТ", "NONE") },
                { "customization.vinyl.1", E("ПОЛОСА", "STRIPE") },
                { "customization.vinyl.2", E("ДВОЙНАЯ", "DOUBLE") },
                { "customization.vinyl.3", E("ТРИО", "TRIO") },
                { "customization.wheel.0", E("СТАНДАРТ", "STANDARD") },
                { "customization.wheel.1", E("СЕРЕБРО", "SILVER") },
                { "customization.wheel.2", E("ГРАФИТ", "GRAPHITE") },
                { "customization.wheel.3", E("ЗОЛОТО", "GOLD") },
                { "customization.neon.0", E("ВЫКЛ", "OFF") },
                { "customization.neon.on", E("ВКЛ", "ON") },
                { "customization.neon.blue", E("ГОЛУБОЙ", "BLUE") },
                { "customization.neon.orange", E("ОРАНЖЕВЫЙ", "ORANGE") },
                { "customization.neon.purple", E("ФИОЛЕТОВЫЙ", "PURPLE") },
                { "customization.neon.green", E("ЗЕЛЁНЫЙ", "GREEN") },
                { "customization.neon.yellow", E("ЖЁЛТЫЙ", "YELLOW") },
                { "customization.preset_slot", E("ПРЕСЕТ {0}", "PRESET {0}") },
                { "customization.preset_selected", E("ВЫБРАН ПРЕСЕТ {0}", "PRESET {0} SELECTED") },
                { "customization.preset_saved", E("ПРЕСЕТ {0} СОХРАНЁН", "PRESET {0} SAVED") },
                { "customization.preset_loaded", E("ПРЕСЕТ {0} ЗАГРУЖЕН", "PRESET {0} LOADED") },
                { "customization.preset_empty", E("ПРЕСЕТ {0} ЕЩЁ ПУСТ", "PRESET {0} IS EMPTY") },
                { "customization.photo_taken", E("ФОТО СОХРАНЕНО", "PHOTO SAVED") },
                { "upgrade.engine", E("МОТОР", "ENGINE") },
                { "upgrade.grip", E("ШИНЫ", "TIRES") },
                { "upgrade.stability", E("ШАССИ", "CHASSIS") },

                { "activity.delivery", E("ДОСТАВКА", "DELIVERY") },
                { "activity.drift", E("ДРИФТ", "DRIFT") },
                { "activity.sprint", E("УЛИЧНЫЙ СПРИНТ", "STREET SPRINT") },
                { "activity.circuit", E("КОЛЬЦЕВАЯ ГОНКА", "CIRCUIT RACE") },
                { "activity.elite_sprint", E("ЭЛИТНЫЙ УЛИЧНЫЙ СПРИНТ", "ELITE STREET SPRINT") },
                { "activity.elite_drift", E("ЭЛИТНЫЙ ДРИФТ", "ELITE DRIFT") },
                { "activity.premium_delivery", E("ПРЕМИУМ-ДОСТАВКА", "PREMIUM DELIVERY") },

                { "discipline.racing", E("ГОНКИ", "RACING") },
                { "discipline.drift", E("ДРИФТ", "DRIFT") },
                { "discipline.delivery", E("ДОСТАВКА", "DELIVERY") },

                { "medal.gold", E("ЗОЛОТО", "GOLD") },
                { "medal.silver", E("СЕРЕБРО", "SILVER") },
                { "medal.bronze", E("БРОНЗА", "BRONZE") },
                { "medal.none", E("БЕЗ МЕДАЛИ", "NO MEDAL") },

                { "career.rookie", E("НОВИЧОК", "ROOKIE") },
                { "career.street_pro", E("УЛИЧНЫЙ ПРОФИ", "STREET PRO") },
                { "career.elite", E("ЭЛИТА", "ELITE") },
                { "career.legend", E("КАРЬЕРА: ЛЕГЕНДА MOTOR CITY", "CAREER: MOTOR CITY LEGEND") },

                { "risk.hud", E("ИНСПЕКТОР • ВНИМАНИЕ {0}/5 • ЕДЬ СПОКОЙНО", "INSPECTOR • ATTENTION {0}/5 • DRIVE CALMLY") },
                { "risk.calm", E("СПОКОЙНО", "CALM") },
                { "risk.active", E("ИНСПЕКТОР СЛЕДИТ", "INSPECTOR WATCHING") },
                { "risk.cleared", E("ИНСПЕКТОР — ВНИМАНИЕ СБРОШЕНО", "INSPECTOR — ATTENTION CLEARED") },
                { "risk.sprint", E("ИНСПЕКТОР БУБЛИК ЗАМЕТИЛ БЫСТРЫЙ СПРИНТ", "INSPECTOR BUBLIK NOTICED THE FAST SPRINT") },
                { "risk.drift", E("ИНСПЕКТОР БУБЛИК ЗАМЕТИЛ ДРИФТ", "INSPECTOR BUBLIK NOTICED THE DRIFT") },
                { "risk.night", E("ИНСПЕКТОР БУБЛИК ЗАМЕТИЛ НОЧНОЙ ЗАЕЗД", "INSPECTOR BUBLIK NOTICED THE NIGHT RUN") },
                { "risk.vehicle_changed", E("СМЕНА МАШИНЫ СНИЗИЛА ВНИМАНИЕ ИНСПЕКТОРА", "CHANGING CARS REDUCED INSPECTOR ATTENTION") },
                { "risk.started", E("ИНСПЕКТОР БУБЛИК НАБЛЮДАЕТ — ЕДЬ СПОКОЙНО ИЛИ ЗАЕЗЖАЙ В ГАРАЖ", "INSPECTOR BUBLIK IS WATCHING — DRIVE CALMLY OR VISIT THE GARAGE") },
                { "risk.escaped", E("ИНСПЕКТОР БУБЛИК ПОТЕРЯЛ ТЕБЯ ИЗ ВИДУ", "INSPECTOR BUBLIK LOST SIGHT OF YOU") },
                { "risk.level", E("ВНИМАНИЕ ИНСПЕКТОРА — УРОВЕНЬ {0}/5", "INSPECTOR ATTENTION — LEVEL {0}/5") },

                { "nightclub.title", E("НОЧНОЙ АВТОКЛУБ", "NIGHT CAR CLUB") },
                { "nightclub.invite", E("ТАЙНАЯ ВСТРЕЧА", "SECRET MEET") },
                { "nightclub.cancelled", E("НОЧНОЙ АВТОКЛУБ — ЗАЕЗД ОТМЕНЁН", "NIGHT CAR CLUB — RUN CANCELLED") },
                { "nightclub.stop", E("НОЧНОЙ АВТОКЛУБ — ОСТАНОВИСЬ ДО 8 КМ/Ч", "NIGHT CAR CLUB — SLOW BELOW 8 KM/H") },

                { "specialization.courier", E("ГОРОДСКОЙ КУРЬЕР", "CITY COURIER") },
                { "specialization.sprint", E("УЛИЧНЫЙ СПРИНТ", "STREET SPRINT") },
                { "specialization.drift", E("ДРИФТ-МАШИНА", "DRIFT MACHINE") },
                { "specialization.circuit", E("СПЕЦИАЛИСТ КОЛЬЦА", "CIRCUIT SPECIALIST") },
                { "specialization.allrounder", E("ЭЛИТНЫЙ УНИВЕРСАЛ", "ELITE ALL-ROUNDER") },
                { "specialization.default", E("УНИВЕРСАЛ", "ALL-ROUNDER") },

                { "history.legend", E("ЛЕГЕНДА", "LEGEND") },
                { "history.cult", E("КУЛЬТОВАЯ", "CULT CLASSIC") },
                { "history.proven", E("ПРОВЕРЕНА", "PROVEN") },
                { "history.broken_in", E("ОБКАТАНА", "BROKEN IN") },
                { "history.new", E("НОВАЯ", "NEW") },
                { "history.style_none", E("СТИЛЬ НЕ ОПРЕДЕЛЁН", "STYLE UNDEFINED") },
                { "history.style_racing", E("СТИЛЬ ГОНКИ", "STYLE RACING") },
                { "history.style_drift", E("СТИЛЬ ДРИФТ", "STYLE DRIFT") },
                { "history.style_delivery", E("СТИЛЬ ДОСТАВКА", "STYLE DELIVERY") },
                { "history.garage_line", E("ИСТОРИЯ: {0:0.0} КМ   •   ПОБЕДЫ {1}   •   ЗАРАБОТАНО {2:N0} КР   •   {3}   •   {4}", "HISTORY: {0:0.0} KM   •   WINS {1}   •   EARNED {2:N0} CR   •   {3}   •   {4}") },

                { "specialization.garage_line", E("СПЕЦИАЛИЗАЦИЯ: {0}   •   {1}", "SPECIALIZATION: {0}   •   {1}") },
                { "specialization.reward", E("{0} — {1}   +{2:N0} КР   +{3} ОПЫТ МАСТЕРСТВА", "{0} — {1}   +{2:N0} CR   +{3} MASTERY XP") },
                { "specialization.courier_desc", E("ДОСТАВКА +35% КР", "DELIVERY +35% CR") },
                { "specialization.sprint_desc", E("СПРИНТ +35% КР, ДОСТАВКА +10%", "SPRINT +35% CR, DELIVERY +10%") },
                { "specialization.drift_desc", E("ДРИФТ +40% КР", "DRIFT +40% CR") },
                { "specialization.circuit_desc", E("КОЛЬЦО +35% КР, СПРИНТ +15%", "CIRCUIT +35% CR, SPRINT +15%") },
                { "specialization.allrounder_desc", E("ВСЕ ОСНОВНЫЕ АКТИВНОСТИ +15% КР", "ALL CORE ACTIVITIES +15% CR") },
                { "specialization.none_desc", E("БЕЗ БОНУСА", "NO BONUS") },

                { "risk.admin", E("ВНИМАНИЕ {0:0}/100 • УРОВЕНЬ {1}/5 • {2}", "ATTENTION {0:0}/100 • LEVEL {1}/5 • {2}") },
                { "risk.test", E("ТЕСТ: ВНИМАНИЕ ИНСПЕКТОРА", "TEST: INSPECTOR ATTENTION") },
                { "risk.reason_level", E("{0}   •   ВНИМАНИЕ {1}/5", "{0}   •   ATTENTION {1}/5") },

                { "career.reward", E("КАРЬЕРА — ЭТАП {0} ЗАВЕРШЁН   +{1:N0} КР   +{2:N0} РЕП", "CAREER — STAGE {0} COMPLETE   +{1:N0} CR   +{2:N0} REP") },
                { "career.hud", E("КАРЬЕРА {0}/{1} — {2}: Д {3}/{4}   ДР {5}/{4}   С {6}/{4}   К {7}/{4}", "CAREER {0}/{1} — {2}: D {3}/{4}   DR {5}/{4}   S {6}/{4}   C {7}/{4}") },

                { "discipline.hud", E("ГОНКИ {0}/10   •   ДРИФТ {1}/10   •   ДОСТАВКА {2}/10", "RACING {0}/10   •   DRIFT {1}/10   •   DELIVERY {2}/10") },
                { "discipline.level_up", E("{0}: УРОВЕНЬ {1}/10   +{2} РЕП", "{0}: LEVEL {1}/10   +{2} REP") },
                { "discipline.reward", E("{0}: +{1} РЕП", "{0}: +{1} REP") },

                { "mastery.level_up", E("{0}: МАСТЕРСТВО УР. {1}/10   +{2} ОПЫТ", "{0}: MASTERY LVL {1}/10   +{2} XP") },

                { "collection.legendary", E("КОЛЛЕКЦИЯ {0:N0} • ЛЕГЕНДАРНЫЙ ГАРАЖ", "COLLECTION {0:N0} • LEGENDARY GARAGE") },
                { "collection.hud", E("КОЛЛЕКЦИЯ {0:N0}/{1:N0} • НАГРАДА {2:N0} КР", "COLLECTION {0:N0}/{1:N0} • REWARD {2:N0} CR") },
                { "collection.garage", E("КОЛЛЕКЦИОННЫЙ РЕЙТИНГ: {0:N0}   •   МАШИН {1}/{2}   •   ЭТАП {3}/{4}", "COLLECTION RATING: {0:N0}   •   CARS {1}/{2}   •   STAGE {3}/{4}") },
                { "collection.reset", E("КОЛЛЕКЦИОННЫЕ НАГРАДЫ СБРОШЕНЫ", "COLLECTION REWARDS RESET") },
                { "collection.reward", E("КОЛЛЕКЦИЯ — ЭТАП {0}/{1}   +{2:N0} КР   +{3:N0} РЕП", "COLLECTION — STAGE {0}/{1}   +{2:N0} CR   +{3:N0} REP") },

                { "hud.distance", E("{0}   {1} М", "{0}   {1} M") },
                { "hud.free_drive", E("СВОБОДНАЯ ЕЗДА", "FREE DRIVE") },
                { "hud.sprint", E("СПРИНТ", "SPRINT") },
                { "hud.circuit", E("КОЛЬЦО", "CIRCUIT") },
                { "hud.drift_spot", E("ДРИФТ-ТОЧКА", "DRIFT SPOT") },
                { "hud.discovery", E("ОТКРЫТИЕ", "DISCOVERY") },
                { "hud.stunt", E("ТРАМПЛИН", "STUNT") },
                { "hud.garage", E("ГАРАЖ", "GARAGE") },
                { "hud.radar", E("РАДАР", "SPEED TRAP") },
                { "hud.result_controls", E("ENTER  ПОВТОРИТЬ     ESC  ПРОДОЛЖИТЬ", "ENTER  RETRY     ESC  CONTINUE") },
                { "hud.no_rewards", E("БЕЗ НАГРАДЫ", "NO REWARD") },
                { "hud.result_reward", E("+{0:N0} КР   +{1:N0} РЕП", "+{0:N0} CR   +{1:N0} REP") },
                { "hud.garage_controls", E("1/2/3 УЛУЧШИТЬ • Z/X МАШИНА • V ЦВЕТ • J ДИСКИ • N НЕОН • E/ESC ЗАКРЫТЬ", "1/2/3 UPGRADE • Z/X CAR • V COLOR • J WHEELS • N NEON • E/ESC CLOSE") },
                { "hud.garage_title", E("ГАРАЖ · УЛУЧШЕНИЯ", "GARAGE · UPGRADES") },
                { "hud.garage_vehicle", E("МАШИНА: {0}", "CAR: {0}") },
                { "hud.first_activity_target", E("ПЕРВАЯ АКТИВНОСТЬ · ДОСТАВКА", "FIRST ACTIVITY · DELIVERY") },
                { "hud.story_delivery_target", E("ДЕЛО · ДОСТАВКА", "JOB · DELIVERY") },
                { "hud.story_drift_target", E("ДЕЛО · ДРИФТ", "JOB · DRIFT") },
                { "hud.story_sprint_target", E("ДЕЛО · СПРИНТ", "JOB · SPRINT") },
                { "hud.story_circuit_target", E("ДЕЛО · КОЛЬЦО", "JOB · CIRCUIT") },
                { "navigator.title", E("НАВИГАТОР", "NAVIGATOR") },
                { "navigator.controls", E("Z / X — ВЫБОР • ENTER — ПРОЛОЖИТЬ МАРШРУТ • ESC / M — ЗАКРЫТЬ", "Z / X — SELECT • ENTER — SET ROUTE • ESC / M — CLOSE") },
                { "navigator.selection", E("{0}/{1}\n{2}", "{0}/{1}\n{2}") },
                { "navigator.empty", E("Нет доступных точек", "No destinations available") },
                { "navigator.touch_button", E("КАРТА", "MAP") },
                { "hud.target", E("ЦЕЛЬ: {0}", "TARGET: {0}") },

                { "garage.marker_text", E("Фиолетовый маркер: гараж", "Purple marker: garage") },
                { "garage.stop_first", E("Гараж: сначала полностью останови машину", "Garage: stop the car first") },
                { "garage.open_cancel", E("E — открыть гараж и отменить «{0}»", "E — open garage and cancel “{0}”") },
                { "garage.stop_and_open", E("ГАРАЖ — остановись и нажми E", "GARAGE — stop and press E") },
                { "garage.open_prompt", E("ГАРАЖ — нажми E", "GARAGE — press E") },
                { "garage.fleet_unavailable", E("Автопарк ещё не подготовлен", "Vehicle roster is not ready yet") },
                { "garage.activity_name", E("Гараж", "Garage") },
                { "garage.max_short", E("МАКС", "MAX") },
                { "garage.level_line", E("УР. {0}/{1}", "LVL {0}/{1}") },
                { "garage.bought", E("КУПЛЕНО", "OWNED") },
                { "garage.price", E("{0:N0} КР", "{0:N0} CR") },
                { "garage.title", E("[{0}] {1}   {2}", "[{0}] {1}   {2}") },
                { "garage.engine_desc", E("ТЮНИНГ МОТОРА: +{0} км/ч, +{1} разгон, +{2}% тяга", "ENGINE TUNING: +{0} km/h, +{1} accel, +{2}% power") },
                { "garage.grip_desc", E("ШИНЫ / СЦЕП: +{0}% сцепления, меньше пробуксовка и стабильнее быстрые повороты", "TIRES / GRIP: +{0}% grip, less wheelspin and more stable fast corners") },
                { "garage.stability_desc", E("ШАССИ: -{0} мм центр массы, +{1}% угловое демпфирование", "CHASSIS: -{0} mm center of mass, +{1}% angular damping") },
                { "garage.upgrade_max", E("{0} уже улучшен до максимума", "{0} is already fully upgraded") },
                { "garage.need_credits2", E("Для «{0}» нужно {1:N0} КР", "“{0}” needs {1:N0} CR") },
                { "garage.upgraded2", E("{0} улучшен до уровня {1}", "{0} upgraded to level {1}") },
                { "garage.engine_name", E("Двигатель", "Engine") },
                { "garage.grip_name", E("Сцепление", "Grip") },
                { "garage.stability_name", E("Стабильность", "Stability") },

                { "activity.marker.delivery", E("Синий маркер: доставка", "Blue marker: delivery") },
                { "activity.marker.sprint", E("Зелёный маркер: уличный спринт", "Green marker: street sprint") },
                { "activity.marker.circuit", E("Бирюзовый флаг: кольцевая гонка", "Cyan flag: circuit race") },
                { "activity.busy", E("{0} недоступно: активно «{1}»", "{0} unavailable: “{1}” is active") },
                { "activity.stop", E("{0} — остановись до {1:0} км/ч", "{0} — slow below {1:0} km/h") },
                { "activity.best_short", E("   РЕК {0:0.0}с", "   BEST {0:0.0}s") },
                { "activity.elite_hint", E("   SHIFT+E — ЭЛИТА", "   SHIFT+E — ELITE") },
                { "activity.premium_hint", E("   SHIFT+E — ПРЕМИУМ", "   SHIFT+E — PREMIUM") },
                { "activity.elite_locked", E("   ЭЛИТА: {0} {1}", "   ELITE: {0} {1}") },
                { "activity.premium_locked", E("   ПРЕМИУМ: {0} {1}", "   PREMIUM: {0} {1}") },
                { "activity.start_time", E("{0}   E — НАЧАТЬ   ЗОЛОТО ≤ {1:0}с{2}{3}", "{0}   E — START   GOLD ≤ {1:0}s{2}{3}") },
                { "activity.countdown", E("{0}   СТАРТ ЧЕРЕЗ {1}   ESC — ОТМЕНА", "{0}   START IN {1}   ESC — CANCEL") },
                { "activity.checkpoint", E("{0}  ТОЧКА {1}/{2}   {3:0.0}с   {4}   ESC — ОТМЕНА", "{0}  POINT {1}/{2}   {3:0.0}s   {4}   ESC — CANCEL") },
                { "activity.tier_time", E("{0} ≤ {1:0}с", "{0} ≤ {1:0}s") },
                { "activity.deliver_cargo", E("ДОСТАВЬ ГРУЗ", "DELIVER THE CARGO") },
                { "activity.finish_now", E("ФИНИШИРУЙ", "FINISH") },
                { "activity.delivered", E("ДОСТАВЛЕНО", "DELIVERED") },
                { "activity.new_record_inline", E("   •   НОВЫЙ РЕКОРД", "   •   NEW RECORD") },
                { "activity.record_inline", E("   •   Рекорд: {0:0.0}с", "   •   Record: {0:0.0}s") },
                { "activity.result_time", E("Время: {0:0.0}с{1}", "Time: {0:0.0}s{1}") },
                { "activity.result_time_bonus", E("Время: {0:0.0}с   •   Бонус: {1:N0} КР{2}", "Time: {0:0.0}s   •   Bonus: {1:N0} CR{2}") },
                { "activity.status_reward", E("{0}: {1}  +{2:N0} КР", "{0}: {1}  +{2:N0} CR") },
                { "activity.delivery_cancelled", E("Доставка отменена", "Delivery cancelled") },
                { "activity.sprint_cancelled", E("Спринт отменён. Отъедь от старта, чтобы повторить.", "Sprint cancelled. Leave the start area to retry.") },
                { "activity.circuit_cancelled", E("Кольцевая гонка отменена. Отъедь от старта, чтобы повторить.", "Circuit race cancelled. Leave the start area to retry.") },
                { "activity.best_lap", E("   •   Лучший круг: {0:0.0}с", "   •   Best lap: {0:0.0}s") },
                { "activity.best_lap_short", E("   ЛУЧШ КРУГ {0:0.0}с", "   BEST LAP {0:0.0}s") },
                { "activity.circuit_status", E("КОЛЬЦО  КРУГ {0}/{1}   ТОЧКА {2}/{3}   КРУГ {4:0.0}с   ОБЩ {5:0.0}с{6}   ESC — ОТМЕНА", "CIRCUIT  LAP {0}/{1}   POINT {2}/{3}   LAP {4:0.0}s   TOTAL {5:0.0}s{6}   ESC — CANCEL") },
                { "activity.circuit_result", E("Время: {0:0.0}с   •   {1}{2}   •   Бонус: {3:N0} КР", "Time: {0:0.0}s   •   {1}{2}   •   Bonus: {3:N0} CR") },

                { "activity.marker.drift", E("Оранжевая зона: дрифт-заезд", "Orange zone: drift challenge") },
                { "activity.drift_challenge", E("ДРИФТ-ЗАЕЗД", "DRIFT CHALLENGE") },
                { "activity.elite_drift_short", E("ЭЛИТНЫЙ ДРИФТ", "ELITE DRIFT") },
                { "activity.drift_start", E("ДРИФТ-ЗАЕЗД   E — НАЧАТЬ   БРОНЗА {0:N0}   ЛЕГЕНДА {1:N0}{2}", "DRIFT CHALLENGE   E — START   BRONZE {0:N0}   LEGEND {1:N0}{2}") },
                { "activity.drift_too_far", E("СЛИШКОМ ДАЛЕКО ОТ ПЛОЩАДКИ", "TOO FAR FROM THE DRIFT ZONE") },
                { "activity.drift_return", E("ДРИФТ  {0:N0}   {1:0.0}с   ВЕРНИСЬ {2:0.0}с", "DRIFT  {0:N0}   {1:0.0}s   RETURN {2:0.0}s") },
                { "activity.drift_status", E("ДРИФТ  {0:N0}   {1}   {2:0.0}с   ESC — ОТМЕНА", "DRIFT  {0:N0}   {1}   {2:0.0}s   ESC — CANCEL") },
                { "activity.score_target", E("{0} {1:N0}", "{0} {1:N0}") },
                { "activity.legend", E("ЛЕГЕНДА", "LEGEND") },
                { "activity.legend_reached", E("ЛЕГЕНДА ДОСТИГНУТА", "LEGEND REACHED") },
                { "activity.not_enough_score", E("НЕ ХВАТИЛО ОЧКОВ: {0:N0}/{1:N0}", "NOT ENOUGH SCORE: {0:N0}/{1:N0}") },
                { "activity.drift_result", E("Очки: {0:N0}   •   Время: {1:0}с", "Score: {0:N0}   •   Time: {1:0}s") },
                { "activity.failed", E("ПРОВАЛ", "FAILED") },
                { "activity.drift_fail_details", E("{0}   •   Очки: {1:N0}", "{0}   •   Score: {1:N0}") },
                { "activity.drift_failed", E("Дрифт-заезд провален", "Drift challenge failed") },
                { "activity.drift_cancelled", E("Дрифт-заезд отменён. Покинь зону, чтобы повторить.", "Drift challenge cancelled. Leave the zone to retry.") },
                { "hud.circuit_lap", E("КОЛЬЦО {0}/{1}", "CIRCUIT {0}/{1}") },

                { "progress.racing_short", E("ГОН", "RACE") },
                { "progress.drift_short", E("ДРИФТ", "DRIFT") },
                { "progress.delivery_short", E("ДОСТ", "DEL") },
                { "progress.night", E("НОЧЬ", "NIGHT") },
                { "progress.ready", E("ГОТОВО", "READY") },

                { "contract.hud", E("КОНТРАКТ {0}/{1} • УР.{2} • {4}: {3} • {5} • {6} • {7:N0} КР", "CONTRACT {0}/{1} • LVL {2} • {4}: {3} • {5} • {6} • {7:N0} CR") },
                { "contract.reset", E("КОНТРАКТЫ СБРОШЕНЫ", "CONTRACTS RESET") },
                { "contract.progress", E("{0}: «{1}» • {2}", "{0}: “{1}” • {2}") },
                { "contract.complete", E("{0}: «{1}» выполнен • +{2:N0} КР • +{3:N0} РЕП. Дальше {4}: «{5}» — {6}", "{0}: “{1}” complete • +{2:N0} CR • +{3:N0} REP. Next {4}: “{5}” — {6}") },
                { "contract.intro", E("ГОРОДСКОЕ ЗНАКОМСТВО", "CITY INTRO") },
                { "contract.racing", E("ГОНОЧНЫЙ ЗАКАЗ", "RACING JOB") },
                { "contract.drift", E("ДРИФТ-КОМИССИЯ", "DRIFT JOB") },
                { "contract.delivery", E("КУРЬЕРСКАЯ СЕРИЯ", "COURIER SERIES") },
                { "contract.night", E("НОЧНАЯ СМЕНА", "NIGHT SHIFT") },
                { "contract.tour", E("БОЛЬШОЙ ГОРОДСКОЙ ТУР", "BIG CITY TOUR") },
                { "contract.brief.intro", E("проверь машину в трёх стилях и покажи, что готов к городу", "test the car in three styles and show you are ready for the city") },
                { "contract.brief.racing", E("Ника готовит фестиваль — помоги проверить гоночные маршруты", "Nika is preparing the festival — help test the race routes") },
                { "contract.brief.drift", E("Нике нужны яркие дрифт-заезды для городского шоу", "Nika needs flashy drift runs for the city show") },
                { "contract.brief.delivery", E("мастерской дяди Вити нужны аккуратные доставки деталей", "Uncle Vitya's workshop needs careful parts deliveries") },
                { "contract.brief.night", E("Бублик просит проверить вечерние маршруты и освещение города", "Bublik asks you to check evening routes and city lighting") },
                { "contract.brief.tour", E("Турбо придумал большой тур: попробуй всё, чему научился", "Turbo planned a big tour: use everything you have learned") },

                { "legend.all_defeated", E("ЛЕГЕНДЫ ГОРОДА • ВСЕ ПОБЕЖДЕНЫ", "CITY LEGENDS • ALL DEFEATED") },
                { "legend.hud", E("ЛЕГЕНДА — {0} • {1}", "LEGEND — {0} • {1}") },
                { "legend.new", E("НОВАЯ ЛЕГЕНДА — {0}   {1}", "NEW LEGEND — {0}   {1}") },
                { "legend.reset", E("ЛЕГЕНДЫ ГОРОДА СБРОШЕНЫ", "CITY LEGENDS RESET") },
                { "legend.all_complete", E("ЛЕГЕНДЫ ГОРОДА ПОБЕЖДЕНЫ   +{0:N0} КР   +{1:N0} РЕП", "CITY LEGENDS DEFEATED   +{0:N0} CR   +{1:N0} REP") },
                { "legend.complete", E("{0} ПОБЕЖДЁН   +{1:N0} КР   +{2:N0} РЕП   СЛЕДУЮЩИЙ: {3}", "{0} DEFEATED   +{1:N0} CR   +{2:N0} REP   NEXT: {3}") },
                { "legend.ghost", E("ПРИЗРАК", "GHOST") },
                { "legend.ghost_intro", E("Ночной гонщик заметил тебя", "A night racer noticed you") },
                { "legend.slider", E("СКОЛЬЗЯЩИЙ", "SLIDER") },
                { "legend.slider_intro", E("Король городского дрифта принимает вызов", "The city drift champion accepts your challenge") },
                { "legend.zero", E("НОЛЬ", "ZERO") },
                { "legend.zero_intro", E("Самый быстрый курьер города оставил маршрут", "The city’s fastest courier left a route") },
                { "legend.crown", E("КОРОНА", "CROWN") },
                { "legend.crown_intro", E("Финальный вызов требует владения всеми стилями", "The final challenge tests every driving style") },
                { "legend.mastery_req", E("НУЖНО МАСТ {0}", "NEED MASTERY {0}") },
                { "legend.racing_req", E(" • ГОНКИ {0}", " • RACING {0}") },
                { "legend.drift_req", E(" • ДРИФТ {0}", " • DRIFT {0}") },
                { "legend.delivery_req", E(" • ДОСТАВКА {0}", " • DELIVERY {0}") },

                { "event.next", E("ГОРОДСКОЕ СОБЫТИЕ • СЛЕДУЮЩЕЕ ЧЕРЕЗ {0}", "CITY EVENT • NEXT IN {0}") },
                { "event.hud", E("ГОРОДСКОЕ СОБЫТИЕ — {0} • {1} • {2} • +{3:N0} КР", "CITY EVENT — {0} • {1} • {2} • +{3:N0} CR") },
                { "event.expired", E("ГОРОДСКОЕ СОБЫТИЕ ЗАВЕРШЕНО — {0}   время вышло", "CITY EVENT ENDED — {0}   time expired") },
                { "event.reset", E("ГОРОДСКИЕ СОБЫТИЯ СБРОШЕНЫ", "CITY EVENTS RESET") },
                { "event.progress", E("ГОРОДСКОЕ СОБЫТИЕ — {0}   {1}", "CITY EVENT — {0}   {1}") },
                { "event.complete", E("ГОРОДСКОЕ СОБЫТИЕ ВЫПОЛНЕНО — {0}   +{1:N0} КР   +{2:N0} РЕП", "CITY EVENT COMPLETE — {0}   +{1:N0} CR   +{2:N0} REP") },
                { "event.new", E("НОВОЕ ГОРОДСКОЕ СОБЫТИЕ — {0}   {1}", "NEW CITY EVENT — {0}   {1}") },
                { "event.street_heat", E("УЛИЧНЫЙ ЖАР", "STREET HEAT") },
                { "event.drift_session", E("ДРИФТ-СЕССИЯ", "DRIFT SESSION") },
                { "event.courier_rush", E("КУРЬЕРСКИЙ РЫВОК", "COURIER RUSH") },
                { "event.midnight", E("ПОЛУНОЧНЫЙ ЗАЕЗД", "MIDNIGHT RUN") },
                { "event.triple", E("ТРОЙНАЯ УГРОЗА", "TRIPLE CHALLENGE") },

                { "nightclub.hud_complete", E("НОЧНОЙ АВТОКЛУБ • ВНУТРЕННИЙ КРУГ", "NIGHT CAR CLUB • INNER CIRCLE") },
                { "nightclub.hud_trust", E("НОЧНОЙ АВТОКЛУБ • ДОВЕРИЕ {0}/{1}", "NIGHT CAR CLUB • TRUST {0}/{1}") },
                { "nightclub.hud_event", E("НОЧНОЙ АВТОКЛУБ — {0} • {1}", "NIGHT CAR CLUB — {0} • {1}") },
                { "nightclub.countdown", E("НОЧНОЙ АВТОКЛУБ   СТАРТ ЧЕРЕЗ {0}   ESC — ОТМЕНА", "NIGHT CAR CLUB   START IN {0}   ESC — CANCEL") },
                { "nightclub.run", E("НОЧНОЙ АВТОКЛУБ — {0}   ТОЧКА {1}/{2}   {3:0.0}с   ESC — ОТМЕНА", "NIGHT CAR CLUB — {0}   POINT {1}/{2}   {3:0.0}s   ESC — CANCEL") },
                { "nightclub.marker_added", E("   МЕТКА ДОБАВЛЕНА НА МИНИКАРТУ", "   MARKER ADDED TO MINIMAP") },
                { "nightclub.busy", E("НОЧНОЙ АВТОКЛУБ НЕДОСТУПЕН: АКТИВНО «{0}»", "NIGHT CAR CLUB UNAVAILABLE: “{0}” IS ACTIVE") },
                { "nightclub.start", E("НОЧНОЙ АВТОКЛУБ — {0}   E — НАЧАТЬ", "NIGHT CAR CLUB — {0}   E — START") },
                { "nightclub.finish", E("{0} — ФИНИШ {1:0.0}с   +{2:N0} КР   +{3} ДОВЕРИЯ", "{0} — FINISH {1:0.0}s   +{2:N0} CR   +{3} TRUST") },
                { "nightclub.reset", E("НОЧНОЙ АВТОКЛУБ — ПРОГРЕСС СБРОШЕН", "NIGHT CAR CLUB — PROGRESS RESET") },
                { "nightclub.rank_up", E("НОЧНОЙ АВТОКЛУБ — {0}   ДОВЕРИЕ {1}", "NIGHT CAR CLUB — {0}   TRUST {1}") },
                { "nightclub.progress", E("НОЧНОЙ АВТОКЛУБ — {0}   {1}", "NIGHT CAR CLUB — {0}   {1}") },
                { "nightclub.complete_final", E("НОЧНОЙ АВТОКЛУБ — ВНУТРЕННИЙ КРУГ   {0} ЗАВЕРШЁН   +{1:N0} КР   +{2} ДОВЕРИЯ", "NIGHT CAR CLUB — INNER CIRCLE   {0} COMPLETE   +{1:N0} CR   +{2} TRUST") },
                { "nightclub.complete", E("{0} ЗАВЕРШЁН   +{1:N0} КР   +{2} ДОВЕРИЯ   СЛЕДУЮЩЕЕ ДОВЕРИЕ: {3}", "{0} COMPLETE   +{1:N0} CR   +{2} TRUST   NEXT TRUST: {3}") },
                { "nightclub.old_port", E("СТАРЫЙ ПОРТ", "OLD PORT") },
                { "nightclub.old_port_invite", E("После заката встречаемся у старого порта.", "Meet at the old port after sunset.") },
                { "nightclub.nameless", E("БЕЗЫМЯННЫЙ ЗАЕЗД", "NAMELESS RUN") },
                { "nightclub.nameless_invite", E("Ника приготовила новый ночной маршрут.", "Nika prepared a new night route.") },
                { "nightclub.club_test", E("ИСПЫТАНИЕ КЛУБА", "CLUB CHALLENGE") },
                { "nightclub.club_test_invite", E("Автоклуб наблюдает. Покажи аккуратный и быстрый заезд.", "The club is watching. Show a clean, fast run.") },
                { "nightclub.inner", E("ВНУТРЕННИЙ КРУГ", "INNER CIRCLE") },
                { "nightclub.inner_invite", E("Последнее приглашение. Сегодня финальный клубный заезд.", "Final invitation. Tonight is the last club run.") },
                { "nightclub.rank.inner", E("ВНУТРЕННИЙ КРУГ", "INNER CIRCLE") },
                { "nightclub.rank.club", E("ЧЛЕН КЛУБА", "CLUB MEMBER") },
                { "nightclub.rank.trusted", E("ДОВЕРЕННЫЙ", "TRUSTED") },
                { "nightclub.rank.noticed", E("ЗАМЕЧЕН", "NOTICED") },
                { "nightclub.rank.unknown", E("НОВИЧОК", "NEWCOMER") },

                { "world.main_north", E("СЕВЕР ГЛАВНОГО РАЙОНА", "NORTH MAIN DISTRICT") },
                { "world.main_west", E("ЗАПАДНАЯ ОКРАИНА", "WEST OUTSKIRTS") },
                { "world.highway_mid", E("СЕРЕДИНА ШОССЕ", "MID HIGHWAY") },
                { "world.remote_east", E("ВОСТОК ДАЛЬНЕГО РАЙОНА", "EAST OUTER DISTRICT") },
                { "world.remote_south", E("ЮГ ДАЛЬНЕГО РАЙОНА", "SOUTH OUTER DISTRICT") },
                { "world.west_turn", E("ЗАПАДНЫЙ ПОВОРОТ", "WEST TURN") },
                { "world.north_quarter", E("СЕВЕРНЫЙ КВАРТАЛ", "NORTH QUARTER") },
                { "world.remote_corner", E("ДАЛЬНИЙ УГОЛ", "OUTER CORNER") },
                { "world.central_avenue", E("ЦЕНТРАЛЬНЫЙ ПРОСПЕКТ", "CENTRAL AVENUE") },
                { "world.highway", E("ШОССЕ", "HIGHWAY") },
                { "world.remote_district", E("ДАЛЬНИЙ РАЙОН", "OUTER DISTRICT") },
                { "world.north_jump", E("СЕВЕРНЫЙ ТРАМПЛИН", "NORTH RAMP") },

                { "discovery.found", E("ОТКРЫТИЕ — {0}   +{1} КР   +{2} РЕП   {3}/{4}", "DISCOVERY — {0}   +{1} CR   +{2} REP   {3}/{4}") },

                { "challenge.short_goals", E("Б {0}   С {1}   З {2}", "B {0}   S {1}   G {2}") },
                { "challenge.new_record", E("   НОВЫЙ РЕКОРД", "   NEW RECORD") },
                { "challenge.best_score", E("   РЕК {0:N0}", "   BEST {0:N0}") },
                { "challenge.best_speed", E("   РЕК {0:0} {1}", "   BEST {0:0} {1}") },
                { "challenge.best_distance", E("   РЕК {0:0.0} М", "   BEST {0:0.0} M") },
                { "challenge.reward", E("   +{0:N0} КР   +{1:N0} РЕП", "   +{0:N0} CR   +{1:N0} REP") },

                { "driftspot.live", E("ДРИФТ-ТОЧКА — {0}   {1:N0}   Б {2:N0}   С {3:N0}   З {4:N0}", "DRIFT SPOT — {0}   {1:N0}   B {2:N0}   S {3:N0}   G {4:N0}") },
                { "driftspot.prompt", E("ДРИФТ-ТОЧКА — {0}   Б {1:N0}   С {2:N0}   З {3:N0}", "DRIFT SPOT — {0}   B {1:N0}   S {2:N0}   G {3:N0}") },
                { "driftspot.result", E("ДРИФТ-ТОЧКА — {0}   {1:N0}   {2}{3}{4}", "DRIFT SPOT — {0}   {1:N0}   {2}{3}{4}") },

                { "speedtrap.status", E("РАДАР — {0}   {1:0} {2}   {3}{4}{5}", "SPEED TRAP — {0}   {1:0} {2}   {3}{4}{5}") },

                { "stunt.airborne", E("ПРЫЖОК — {0}   В ВОЗДУХЕ", "JUMP — {0}   AIRBORNE") },
                { "stunt.prompt", E("ТРАМПЛИН — {0}   Б {1:0}м   С {2:0}м   З {3:0}м", "RAMP — {0}   B {1:0}m   S {2:0}m   G {3:0}m") },
                { "stunt.live", E("ПРЫЖОК — {0}   {1:0.0} М   {2:0.00} С", "JUMP — {0}   {1:0.0} M   {2:0.00} S") },
                { "stunt.result", E("ПРЫЖОК — {0}   {1:0.0} М   {2:0.00} С   {3}{4}{5}", "JUMP — {0}   {1:0.0} M   {2:0.00} S   {3}{4}{5}") },

                { "adventure.city_event", E("ГОРОДСКОЕ СОБЫТИЕ", "CITY EVENT") },
                { "adventure.contract", E("КОНТРАКТ", "CONTRACT") },
                { "adventure.career", E("КАРЬЕРА", "CAREER") },
                { "adventure.inspector", E("ИНСПЕКТОР БУБЛИК", "INSPECTOR BUBLIK") },

                { "boot.connecting", E("Подключение платформы...", "Connecting platform...") },
                { "boot.sync", E("Синхронизация прогресса...", "Syncing progress...") },
                { "boot.loading", E("Загрузка города...", "Loading city...") },
                { "boot.error", E("Ошибка загрузки города", "City loading error") },

                { "turbo.title", E("ТУРБО", "TURBO") },
                { "turbo.hello", E("Турбо: Привет! Я твой напарник. Поехали исследовать Motor City!", "Turbo: Hi! I’m your co-pilot. Let’s explore Motor City!") },
                { "turbo.reunion", E("Турбо: Я скучал! Ничего не потеряно — просто поехали кататься.", "Turbo: I missed you! Nothing was lost — let’s go for a drive.") },
                { "turbo.mood_happy", E("РАДОСТНЫЙ", "HAPPY") },
                { "turbo.level_up", E("Турбо получил уровень {0}! Новые способности становятся сильнее.", "Turbo reached level {0}! His abilities are getting stronger.") },
                { "turbo.daily_any", E("ТУРБО • Выполни любые активности: {0}/{1}", "TURBO • Complete any activities: {0}/{1}") },
                { "turbo.daily_race", E("ТУРБО • Заверши гонку: {0}/{1}", "TURBO • Finish a race: {0}/{1}") },
                { "turbo.daily_drift", E("ТУРБО • Заверши дрифт-заезд: {0}/{1}", "TURBO • Finish a drift challenge: {0}/{1}") },
                { "turbo.daily_done", E("ТУРБО • Задание на сегодня выполнено", "TURBO • Today’s task is complete") },
                { "turbo.daily_progress", E("Турбо: Отлично! Задание {0}/{1}.", "Turbo: Nice! Task progress {0}/{1}.") },
                { "turbo.daily_reward", E("Турбо: Задание готово! +{0:N0} КР и опыт Турбо.", "Turbo: Task complete! +{0:N0} CR and Turbo XP.") },
                { "turbo.new_daily", E("Турбо: Новое приключение на сегодня готово!", "Turbo: Today’s new adventure is ready!") },
                { "turbo.hint", E("Турбо: Не спеши — цель уже отмечена на миникарте.", "Turbo: No rush — your target is already marked on the minimap.") },
                { "turbo.boost", E("Турбо: Пуск! Короткий буст активирован.", "Turbo: Go! Short boost activated.") },
                { "turbo.garage_skin", E("ТУРБО • СКИН: {0}", "TURBO • SKIN: {0}") },
                { "turbo.skin_classic", E("КЛАССИКА", "CLASSIC") },
                { "turbo.skin_blue", E("НЕОН-СИНИЙ", "NEON BLUE") },
                { "turbo.skin_orange", E("АПЕЛЬСИН", "ORANGE") },
                { "turbo.skin_purple", E("КОСМОС", "COSMIC") },
                { "turbo.skin_gold", E("ЗОЛОТОЙ", "GOLD") },
                { "turbo.skin_season1", E("МЯТНЫЙ ФЕСТИВАЛЬ", "MINT FESTIVAL") },
                { "turbo.skin_premium_1", E("РОЗОВЫЙ ИМПУЛЬС", "PINK PULSE") },
                { "turbo.skin_premium_2", E("МЯТНЫЙ НЕОН", "MINT NEON") },
                { "turbo.skin_premium_3", E("УЛЬТРАФИОЛЕТ", "ULTRAVIOLET") },
                { "turbo.skin_cosmetic_pack", E("ТУРБО-ИСКРА", "TURBO SPARK") },
                { "turbo.skin_unlocked", E("Турбо: открыт новый скин «{0}»!", "Turbo: new skin “{0}” unlocked!") },
                { "turbo.skin_selected", E("Турбо: выбран скин «{0}».", "Turbo: “{0}” skin selected.") },

                { "onboarding.title", E("ПЕРВЫЕ ШАГИ", "FIRST STEPS") },
                { "onboarding.welcome", E("Добро пожаловать в Motor City! Сначала просто почувствуй машину.", "Welcome to Motor City! First, get a feel for the car.") },
                { "onboarding.throttle", E("Нажми газ и начни движение", "Press throttle and start moving") },
                { "onboarding.good_throttle", E("Отлично! Теперь попробуй повернуть.", "Great! Now try steering.") },
                { "onboarding.steer", E("Поверни влево или вправо во время движения", "Steer left or right while moving") },
                { "onboarding.good_steer", E("Управление готово. Прокатись ещё немного!", "Controls ready. Drive a little farther!") },
                { "onboarding.drive", E("Прокатись по городу: {0}/80 м", "Drive around the city: {0}/80 m") },
                { "onboarding.first_reward", E("Первая поездка! +250 КР. Это награда за исследование.", "First drive! +250 CR. That’s your exploration reward.") },
                { "onboarding.meet_turbo", E("Турбо уже с тобой на машине — ехать никуда не нужно.", "Turbo is already riding with you — no need to drive anywhere.") },
                { "onboarding.turbo_ready", E("Турбо с тобой. Теперь следуй к синему маркеру ДОСТАВКА.", "Turbo is with you. Now follow the blue DELIVERY marker.") },
                { "onboarding.activity", E("Едь к синему маркеру ДОСТАВКА, остановись и нажми кнопку действия", "Drive to the blue DELIVERY marker, stop and use the action button") },
                { "onboarding.activity_done", E("Готово! Награды за активности улучшают твою коллекцию.", "Done! Activity rewards grow your collection.") },
                { "onboarding.garage", E("Заедь в гараж и открой его", "Drive to the garage and open it") },
                { "onboarding.garage_done", E("Гараж найден. Теперь выбери другой цвет кузова.", "Garage found. Now choose a different body color.") },
                { "onboarding.customize", E("В гараже измени цвет машины", "Change the car color in the garage") },
                { "onboarding.customized", E("Отлично! Теперь это уже твоя машина.", "Great! Now this car feels like yours.") },
                { "onboarding.daily", E("Посмотри ежедневное задание Турбо", "Check Turbo’s daily task") },
                { "onboarding.complete", E("Первые шаги готовы! Теперь выбирай приключение сам.", "First steps complete! Now choose your own adventure.") },
            { "touch.garage.prev", E("АВТО ◀", "CAR ◀") },
            { "touch.garage.next", E("АВТО ▶", "CAR ▶") },
            { "touch.garage.buy", E("КУПИТЬ", "BUY") },
            { "touch.garage.engine", E("МОТОР", "ENGINE") },
            { "touch.garage.grip", E("СЦЕП.", "GRIP") },
            { "touch.garage.stability", E("СТАБ.", "STAB.") },
            { "touch.garage.color", E("ЦВЕТ", "COLOR") },
            { "touch.garage.wheels", E("ДИСКИ", "WHEELS") },
            { "touch.garage.neon", E("НЕОН", "NEON") },
            { "touch.garage.close", E("ЗАКРЫТЬ", "CLOSE") },
            { "touch.garage.passport", E("ПАСПОРТ", "PASSPORT") },
            { "touch.utility.photo", E("ФОТО", "PHOTO") },
            { "touch.utility.club", E("КЛУБ", "CLUB") },
            { "touch.utility.store", E("МАГАЗИН", "STORE") },
            { "touch.utility.bonus", E("БОНУС", "BONUS") },
            { "touch.modal.prev", E("◀", "◀") },
            { "touch.modal.next", E("▶", "▶") },
            { "touch.modal.select", E("ВЫБРАТЬ", "SELECT") },
            { "touch.modal.close", E("ЗАКРЫТЬ", "CLOSE") },
            { "touch.store.buy", E("КУПИТЬ", "BUY") },
            { "touch.club.join", E("ВЫБРАТЬ", "SELECT") },
            { "touch.drive.action", E("ДЕЙСТВИЕ", "ACTION") },
            { "touch.drive.handbrake", E("РУЧНИК", "HANDBRAKE") },
            { "touch.drive.rescue", E("СПАСТИ", "RESCUE") },
            { "touch.drive.throttle", E("ГАЗ", "GAS") },
            { "touch.drive.brake", E("ТОРМ / НАЗАД", "BRAKE / REV") },
            { "touch.drive.handbrake_short", E("РУЧНИК", "HANDBRAKE") },
            { "touch.drive.action_short", E("ДЕЙСТВИЕ", "ACTION") },
            { "touch.drive.rescue_short", E("СПАСТИ", "RESCUE") },

                { "daily.title", E("ЕЖЕДНЕВНЫЕ ПРИКЛЮЧЕНИЯ", "DAILY ADVENTURES") },
                { "daily.any", E("ПРИКЛЮЧЕНИЕ • Выполни активности: {0}/{1}", "ADVENTURE • Complete activities: {0}/{1}") },
                { "daily.race", E("ПРИКЛЮЧЕНИЕ • Заверши гонку: {0}/{1}", "ADVENTURE • Finish a race: {0}/{1}") },
                { "daily.drift", E("ПРИКЛЮЧЕНИЕ • Заверши дрифт: {0}/{1}", "ADVENTURE • Finish a drift challenge: {0}/{1}") },
                { "daily.delivery", E("ПРИКЛЮЧЕНИЕ • Выполни доставку: {0}/{1}", "ADVENTURE • Complete a delivery: {0}/{1}") },
                { "daily.task_done", E("Ежедневное задание {0}/3 готово! +{1:N0} КР", "Daily task {0}/3 complete! +{1:N0} CR") },
                { "daily.all_tasks", E("Все три задания выполнены", "All three tasks are complete") },
                { "daily.all_done", E("Сегодня всё готово • завершённых дней: {0}", "All done today • completed days: {0}") },
                { "daily.complete", E("Три приключения готовы! +{0:N0} КР • серия {1}", "Three adventures complete! +{0:N0} CR • series {1}") },
                { "daily.milestone", E("Награда за {0} дней! Сегодня +{1:N0} КР", "{0}-day reward! +{1:N0} CR today") },
                { "daily.new_day", E("Новый день — три новых приключения уже готовы.", "New day — three new adventures are ready.") },

                { "story.title", E("ИСТОРИЯ MOTOR CITY", "MOTOR CITY STORY") },
                { "story.hud", E("ИСТОРИЯ {0}/{1} • {2}: {3} • {4}/{5}", "STORY {0}/{1} • {2}: {3} • {4}/{5}") },
                { "story.progress", E("{0}: отлично! Прогресс {1}/{2}.", "{0}: great! Progress {1}/{2}.") },
                { "story.new_mission", E("МИССИЯ {0} • {1} — «{2}»", "MISSION {0} • {1} — “{2}”") },
                { "story.mission_complete", E("МИССИЯ {0} ГОТОВА • {1} • +{2:N0} КР • +{3:N0} РЕП", "MISSION {0} COMPLETE • {1} • +{2:N0} CR • +{3:N0} REP") },
                { "story.final_complete", E("ФЕСТИВАЛЬ MOTOR CITY ЗАВЕРШЁН! +{0:N0} КР • +{1:N0} РЕП", "MOTOR CITY FESTIVAL COMPLETE! +{0:N0} CR • +{1:N0} REP") },
                { "story.complete_hud", E("ИСТОРИЯ • ФЕСТИВАЛЬ ПРОЙДЕН", "STORY • FESTIVAL COMPLETE") },

                { "story.character.vitya", E("ДЯДЯ ВИТЯ", "UNCLE VITYA") },
                { "story.character.nika", E("НИКА", "NIKA") },
                { "story.character.bublik", E("ИНСПЕКТОР БУБЛИК", "INSPECTOR BUBLIK") },
                { "story.character.turbo", E("ТУРБО", "TURBO") },

                { "story.01.title", E("ПЕРВОЕ ДЕЛО", "FIRST JOB") },
                { "story.01.objective", E("едь к синему маркеру ДОСТАВКА, остановись и нажми E", "drive to the blue DELIVERY marker, stop and press E") },
                { "story.02.title", E("ГОРОД ЗОВЁТ", "THE CITY CALLS") },
                { "story.02.objective", E("заверши любую городскую активность", "complete any city activity") },
                { "story.03.title", E("ПОЙМАЙ УГОЛ", "CATCH THE ANGLE") },
                { "story.03.objective", E("заверши дрифт-заезд", "finish a drift challenge") },
                { "story.04.title", E("ПРОВЕРКА МОТОРА", "ENGINE CHECK") },
                { "story.04.objective", E("заверши уличный спринт", "finish a street sprint") },
                { "story.05.title", E("АККУРАТНЫЙ МАРШРУТ", "CLEAN ROUTE") },
                { "story.05.objective", E("выполни спокойную доставку", "complete a careful delivery") },
                { "story.06.title", E("ДВА ПРИКЛЮЧЕНИЯ", "TWO ADVENTURES") },
                { "story.06.objective", E("заверши две любые активности", "complete any two activities") },
                { "story.07.title", E("ПЕРВОЕ КОЛЬЦО", "FIRST CIRCUIT") },
                { "story.07.objective", E("финишируй в кольцевой гонке", "finish a circuit race") },
                { "story.08.title", E("ГОРОДСКОЙ МАСТЕР", "CITY MASTER") },
                { "story.08.objective", E("заверши ещё две активности", "complete two more activities") },
                { "story.09.title", E("КОНТРОЛЬНЫЙ ЗАЕЗД", "CHECK RUN") },
                { "story.09.objective", E("заверши спринт перед фестивалем", "finish a sprint before the festival") },
                { "story.10.title", E("ФЕСТИВАЛЬ MOTOR CITY", "MOTOR CITY FESTIVAL") },
                { "story.10.objective", E("финишируй в финальной кольцевой гонке", "finish the final circuit race") },

                { "season1.title", E("СЕЗОН 1 • БОЛЬШОЙ ФЕСТИВАЛЬ", "SEASON 1 • BIG FESTIVAL") },
                { "season1.welcome", E("СЕЗОН 1 НАЧАЛСЯ • до конца {0} дн. • 10 миссий и уникальный скин Турбо", "SEASON 1 STARTED • {0} days left • 10 missions and a unique Turbo skin") },
                { "season1.hud", E("СЕЗОН 1 • МИССИЯ {0}/{1} • {2} • {3}/{4} • {5} ДН.", "SEASON 1 • MISSION {0}/{1} • {2} • {3}/{4} • {5} DAYS") },
                { "season1.progress", E("СЕЗОН • {0} • {1}/{2}", "SEASON • {0} • {1}/{2}") },
                { "season1.mission_complete", E("СЕЗОН • МИССИЯ {0} ГОТОВА • +{1:N0} КР • +{2:N0} РЕП", "SEASON • MISSION {0} COMPLETE • +{1:N0} CR • +{2:N0} REP") },
                { "season1.final", E("СЕЗОН 1 ПРОЙДЕН! +{0:N0} КР • +{1:N0} РЕП • открыт скин Турбо «Мятный фестиваль»", "SEASON 1 COMPLETE! +{0:N0} CR • +{1:N0} REP • Turbo skin “Mint Festival” unlocked") },
                { "season1.complete_hud", E("СЕЗОН 1 • ВСЕ МИССИИ ПРОЙДЕНЫ", "SEASON 1 • ALL MISSIONS COMPLETE") },
                { "season1.ended", E("СЕЗОН 1 ЗАВЕРШЁН", "SEASON 1 ENDED") },
                { "season1.m1", E("ПЕРВАЯ ДОСТАВКА ФЕСТИВАЛЯ", "FIRST FESTIVAL DELIVERY") },
                { "season1.m2", E("ПИЦЦА ДЛЯ КОМАНДЫ", "PIZZA FOR THE CREW") },
                { "season1.m3", E("ДРИФТ-РЕПЕТИЦИЯ НИКИ", "NIKA'S DRIFT REHEARSAL") },
                { "season1.m4", E("БЫСТРЫЙ ПРОГОН", "FAST RUN") },
                { "season1.m5", E("ГОСТИ ФЕСТИВАЛЯ", "FESTIVAL GUESTS") },
                { "season1.m6", E("ПРОВЕРКА КОЛЬЦА", "CIRCUIT CHECK") },
                { "season1.m7", E("ПОЧТА ОРГАНИЗАТОРОВ", "ORGANIZER MAIL") },
                { "season1.m8", E("МАШИНА ДОЛЖНА СИЯТЬ", "MAKE IT SHINE") },
                { "season1.m9", E("ТРИ ГОРОДСКИХ ПРИКЛЮЧЕНИЯ", "THREE CITY ADVENTURES") },
                { "season1.m10", E("ФИНАЛЬНЫЙ КУБОК", "FINAL CUP") },

                { "achievement.unlocked", E("ДОСТИЖЕНИЕ «{0}» • +{1:N0} КР • +{2:N0} РЕП • {3}/{4}", "ACHIEVEMENT “{0}” • +{1:N0} CR • +{2:N0} REP • {3}/{4}") },
                { "achievement.first_drive", E("ПЕРВЫЙ УСПЕХ", "FIRST SUCCESS") },
                { "achievement.racer", E("ГОРОДСКОЙ ГОНЩИК", "CITY RACER") },
                { "achievement.drifter", E("МАСТЕР УГЛА", "ANGLE MASTER") },
                { "achievement.worker", E("ПОМОЩНИК ГОРОДА", "CITY HELPER") },
                { "achievement.explorer", E("ИССЛЕДОВАТЕЛЬ", "EXPLORER") },
                { "achievement.photographer", E("ФОТОГРАФ MOTOR CITY", "MOTOR CITY PHOTOGRAPHER") },
                { "achievement.collector", E("КОЛЛЕКЦИОНЕР", "COLLECTOR") },
                { "achievement.story", E("ЮНАЯ ЛЕГЕНДА", "YOUNG LEGEND") },
                { "achievement.season", E("ЧЕМПИОН СЕЗОНА", "SEASON CHAMPION") },

                { "photo.album_line", E("ФОТОАЛЬБОМ • МЕСТА {0}/{1} • СЕКРЕТЫ {2}/{3} • РЕДКИЕ МАШИНЫ {4}/{5} • СЕЗОН {6}/{7}", "PHOTO ALBUM • PLACES {0}/{1} • SECRETS {2}/{3} • RARE CARS {4}/{5} • SEASON {6}/{7}") },
                { "photo.captured", E("ФОТООХОТА — {0} • +{1:N0} КР • +{2:N0} РЕП", "PHOTO HUNT — {0} • +{1:N0} CR • +{2:N0} REP") },
                { "photo.landmark_name", E("ДОСТОПРИМЕЧАТЕЛЬНОСТЬ: {0}", "LANDMARK: {0}") },
                { "photo.secret_name", E("СЕКРЕТНОЕ МЕСТО #{0}", "SECRET PLACE #{0}") },
                { "photo.rare_name", E("РЕДКАЯ МАШИНА #{0}", "RARE CAR #{0}") },
                { "photo.seasonal_name", E("СЕЗОННЫЙ КАДР", "SEASONAL SHOT") },
                { "photo.rare_duplicate", E("Эта редкая машина уже есть в альбоме", "This rare car is already in the album") },
                { "photo.no_subject", E("Турбо: попробуй взять в кадр интересное место или машину.", "Turbo: try framing an interesting place or car.") },

                { "profession.title", E("ГОРОДСКИЕ ПРОФЕССИИ", "CITY JOBS") },
                { "profession.pizza", E("ДОСТАВКА ПИЦЦЫ", "PIZZA DELIVERY") },
                { "profession.taxi", E("ТАКСИ", "TAXI") },
                { "profession.mail", E("ГОРОДСКАЯ ПОЧТА", "CITY MAIL") },
                { "profession.icecream", E("МОРОЖЕНОЕ", "ICE CREAM ROUTE") },
                { "profession.start", E("{0} • УР. ПРОФЕССИЙ {1} • НАГРАДА ОТ {2:N0} КР • E — НАЧАТЬ", "{0} • JOB LVL {1} • REWARD FROM {2:N0} CR • E — START") },
                { "profession.stop", E("{0} • остановись, чтобы начать работу", "{0} • stop to start the job") },
                { "profession.busy", E("{0} недоступно: сначала заверши «{1}»", "{0} unavailable: finish “{1}” first") },
                { "profession.started", E("{0} • маршрут начат", "{0} • route started") },
                { "profession.progress", E("{0} • ТОЧКА {1}/{2} • {3} м • {4}", "{0} • STOP {1}/{2} • {3} m • {4}") },
                { "profession.complete", E("РАБОТА ВЫПОЛНЕНА", "JOB COMPLETE") },
                { "profession.result", E("ВРЕМЯ {0} • УР. ПРОФЕССИЙ {1}", "TIME {0} • JOB LVL {1}") },
                { "profession.cancelled", E("{0} • работа отменена", "{0} • job cancelled") },

                { "carwash.title", E("АВТОМОЙКА", "CAR WASH") },
                { "carwash.start", E("АВТОМОЙКА • остановись и нажми E", "CAR WASH • stop and press E") },
                { "carwash.stop", E("АВТОМОЙКА • сначала остановись", "CAR WASH • stop first") },
                { "carwash.busy", E("АВТОМОЙКА недоступна: заверши «{0}»", "CAR WASH unavailable: finish “{0}”") },
                { "carwash.phase.water", E("ВОДА", "RINSE") },
                { "carwash.phase.foam", E("ПЕНА", "FOAM") },
                { "carwash.phase.polish", E("ПОЛИРОВКА", "POLISH") },
                { "carwash.progress", E("МОЙКА {0}/3 • {1} • УДЕРЖИВАЙ E • {2}%", "WASH {0}/3 • {1} • HOLD E • {2}%") },
                { "carwash.complete", E("МАШИНА СИЯЕТ!", "CAR IS SHINING!") },
                { "carwash.result", E("УР. ПРОФЕССИЙ {0}", "JOB LVL {0}") },
                { "carwash.cancelled", E("МОЙКА ОТМЕНЕНА", "CAR WASH CANCELLED") },

                { "tow.title", E("СЛУЖБА ЭВАКУАЦИИ", "TOW SERVICE") },
                { "tow.start", E("ЭВАКУАТОР • УР. ПРОФЕССИЙ {0} • E — ПРИНЯТЬ ВЫЗОВ", "TOW SERVICE • JOB LVL {0} • E — ACCEPT CALL") },
                { "tow.stop", E("ЭВАКУАТОР • остановись, чтобы принять вызов", "TOW SERVICE • stop to accept the call") },
                { "tow.busy", E("ЭВАКУАТОР недоступен: заверши «{0}»", "TOW SERVICE unavailable: finish “{0}”") },
                { "tow.go_pickup", E("Турбо: заглохшая машина отмечена на миникарте. Едем помогать!", "Turbo: the stalled car is marked on the minimap. Let’s help!") },
                { "tow.pickup_distance", E("ЭВАКУАТОР • до машины {0} м", "TOW SERVICE • {0} m to the car") },
                { "tow.pickup_stop", E("ЭВАКУАТОР • остановись рядом с машиной", "TOW SERVICE • stop beside the car") },
                { "tow.hook_prompt", E("ЭВАКУАТОР • E — ПОДЦЕПИТЬ МАШИНУ", "TOW SERVICE • E — ATTACH CAR") },
                { "tow.hooking", E("ПОДКЛЮЧЕНИЕ БУКСИРА • УДЕРЖИВАЙ E • {0}%", "ATTACHING TOW • HOLD E • {0}%") },
                { "tow.deliver", E("Готово! Аккуратно отвези машину в сервис.", "Ready! Carefully bring the car to service.") },
                { "tow.delivery_distance", E("ЭВАКУАТОР • до сервиса {0} м", "TOW SERVICE • {0} m to service") },
                { "tow.service_stop", E("ЭВАКУАТОР • остановись в сервисной зоне", "TOW SERVICE • stop in the service zone") },
                { "tow.complete", E("МАШИНА ДОСТАВЛЕНА В СЕРВИС", "CAR DELIVERED TO SERVICE") },
                { "tow.result", E("ПОМОЩЬ ГОРОДУ • УР. ПРОФЕССИЙ {0}", "CITY ASSIST • JOB LVL {0}") },
                { "tow.cancelled", E("ВЫЗОВ ЭВАКУАТОРА ОТМЕНЁН", "TOW CALL CANCELLED") },

                { "club.title", E("АВТОКЛУБ MOTOR CITY", "MOTOR CITY CLUB") },
                { "club.none", E("БЕЗ КЛУБА", "NO CLUB") },
                { "club.join_prompt", E("Выбери клуб и вступи — без чата и лишних сообщений", "Choose a club and join — no open chat") },
                { "club.controls_join", E("Z / X — ВЫБОР   •   E / ENTER — ВСТУПИТЬ   •   ESC / U — ЗАКРЫТЬ", "Z / X — SELECT   •   E / ENTER — JOIN   •   ESC / U — CLOSE") },
                { "club.controls_member", E("Z / X — ДРУГИЕ КЛУБЫ   •   E / ENTER — СМЕНИТЬ КЛУБ   •   ESC / U — ЗАКРЫТЬ", "Z / X — OTHER CLUBS   •   E / ENTER — SWITCH CLUB   •   ESC / U — CLOSE") },
                { "club.joined", E("Ты теперь в клубе «{0}»!", "You joined “{0}”!") },
                { "club.weekly_progress", E("НЕДЕЛЬНАЯ ЦЕЛЬ • ЛИЧНЫЙ ВКЛАД {0}/{1}", "WEEKLY GOAL • PERSONAL CONTRIBUTION {0}/{1}") },
                { "club.weekly_done", E("НЕДЕЛЬНАЯ ЦЕЛЬ ВЫПОЛНЕНА • {0}/{1}", "WEEKLY GOAL COMPLETE • {0}/{1}") },
                { "club.contribution", E("{0} • вклад в неделю {1}/{2}", "{0} • weekly contribution {1}/{2}") },
                { "club.weekly_reward", E("{0} • недельная цель готова! +{1:N0} КР • +{2:N0} РЕП", "{0} • weekly goal complete! +{1:N0} CR • +{2:N0} REP") },

                { "club.name.neon", E("НЕОНОВЫЕ КОЛЁСА", "NEON WHEELS") },
                { "club.desc.neon", E("Любят ночной город, яркие машины и аккуратный стиль.", "Night-city fans with bright cars and clean style.") },
                { "club.name.turbo", E("ДРУЗЬЯ ТУРБО", "TURBO FRIENDS") },
                { "club.desc.turbo", E("Исследуют город вместе с Турбо и собирают коллекции.", "Explore the city with Turbo and collect everything.") },
                { "club.name.sun", E("СОЛНЕЧНЫЙ ГАРАЖ", "SUNNY GARAGE") },
                { "club.desc.sun", E("Спокойные поездки, красивые машины и помощь городу.", "Relaxed drives, good-looking cars and helping the city.") },
                { "club.name.rainbow", E("РАДУЖНЫЙ ДРИФТ", "RAINBOW DRIFT") },
                { "club.desc.rainbow", E("Яркий дрифт без токсичности и опасных выходок.", "Bright drifting without toxicity or dangerous behavior.") },
                { "club.name.city", E("ГОРОДСКИЕ ЛИСЫ", "CITY FOXES") },
                { "club.desc.city", E("Знают каждый район, секрет и короткий маршрут.", "Know every district, secret and shortcut.") },
                { "club.name.spark", E("КОМАНДА ИСКРА", "TEAM SPARK") },
                { "club.desc.spark", E("Соревнуются дружелюбно и помогают новичкам.", "Friendly competition and helping newcomers.") },

                { "weekend.drift", E("ДРИФТ-ФЕСТИВАЛЬ", "DRIFT FESTIVAL") },
                { "weekend.helpers", E("ГОРОДСКИЕ ПОМОЩНИКИ", "CITY HELPERS") },
                { "weekend.speed", E("СКОРОСТНЫЕ ВЫХОДНЫЕ", "SPEED WEEKEND") },
                { "weekend.hud", E("ВЫХОДНЫЕ • {0} • БОНУС +{1:N0} КР", "WEEKEND • {0} • BONUS +{1:N0} CR") },
                { "weekend.started", E("СОБЫТИЕ ВЫХОДНОГО ДНЯ: {0} • +{1:N0} КР ЗА ПОДХОДЯЩУЮ АКТИВНОСТЬ", "WEEKEND EVENT: {0} • +{1:N0} CR FOR MATCHING ACTIVITIES") },
                { "weekend.reward", E("{0} • БОНУС ВЫХОДНОГО ДНЯ +{1:N0} КР", "{0} • WEEKEND BONUS +{1:N0} CR") },

                { "rewarded.prompt", E("Y — добровольный рекламный бонус +{0:N0} КР • сегодня {1}/{2}", "Y — optional rewarded bonus +{0:N0} CR • today {1}/{2}") },
                { "rewarded.opening", E("Открываю добровольный бонус...", "Opening optional rewarded bonus...") },
                { "rewarded.received", E("БОНУС ПОЛУЧЕН • +{0:N0} КР", "BONUS RECEIVED • +{0:N0} CR") },
                { "rewarded.no_reward", E("Бонус не получен — прогресс не изменился", "No reward received — progress unchanged") },
                { "rewarded.limit", E("Рекламные бонусы на сегодня закончились", "Rewarded bonuses are finished for today") },
                { "rewarded.cooldown", E("Следующий добровольный бонус через {0} с", "Next optional bonus in {0} s") },
                { "rewarded.yandex_only", E("Добровольный рекламный бонус доступен в Yandex Games", "Optional rewarded bonus is available in Yandex Games") },

                { "store.title", E("МАГАЗИН КОСМЕТИКИ", "COSMETIC STORE") },
                { "store.season.name", E("ПРЕМИУМ-ПУТЬ СЕЗОНА 1", "SEASON 1 PREMIUM PATH") },
                { "store.season.desc", E("Дополнительные косметические награды за прогресс сезона. Не влияет на скорость и победы.", "Extra cosmetic rewards for season progress. Does not affect speed or wins.") },
                { "store.turbo.name", E("НАБОР ТУРБО", "TURBO PACK") },
                { "store.turbo.desc", E("Эксклюзивный косметический скин Турбо. Без игровых преимуществ.", "Exclusive cosmetic Turbo skin. No gameplay advantage.") },
                { "store.owned", E("УЖЕ КУПЛЕНО", "OWNED") },
                { "store.buy", E("E — КУПИТЬ", "E — BUY") },
                { "store.yandex_only", E("Покупки доступны только в Yandex Games", "Purchases are available only in Yandex Games") },
                { "store.premium_active", E("ПРЕМИУМ АКТИВЕН", "PREMIUM ACTIVE") },
                { "store.premium_locked", E("ПРЕМИУМ НЕ АКТИВЕН", "PREMIUM NOT ACTIVE") },
                { "store.season_path", E("СЕЗОН {0}/{1} • {2}", "SEASON {0}/{1} • {2}") },
                { "store.already_owned", E("Этот косметический набор уже куплен", "This cosmetic pack is already owned") },
                { "store.opening", E("Открываю безопасную покупку Yandex Games...", "Opening secure Yandex Games purchase...") },
                { "store.cancelled", E("Покупка отменена — прогресс не изменился", "Purchase cancelled — progress unchanged") },
                { "store.granted", E("ПОКУПКА ГОТОВА • {0}", "PURCHASE COMPLETE • {0}") },
                { "store.controls", E("T — ЗАКРЫТЬ • Z/X — ВЫБОР • E — КУПИТЬ", "T — CLOSE • Z/X — SELECT • E — BUY") },
                { "store.status", E("{0}\n{1}\n{2}\n{3}", "{0}\n{1}\n{2}\n{3}") }
            };

        private static readonly Dictionary<string, LocalizedEntry> TouchPromptOverrides =
            new()
            {
                { "garage.prompt", E("ГАРАЖ — ДЕЙСТВИЕ", "GARAGE — ACTION") },
                { "garage.open_cancel", E("ДЕЙСТВИЕ — открыть гараж и отменить «{0}»", "ACTION — open garage and cancel “{0}”") },
                { "garage.stop_and_open", E("ГАРАЖ — остановись и нажми ДЕЙСТВИЕ", "GARAGE — stop and press ACTION") },
                { "garage.open_prompt", E("ГАРАЖ — ДЕЙСТВИЕ", "GARAGE — ACTION") },
                { "hud.garage_controls", E("Используй кнопки гаража ниже", "Use the garage buttons below") },
                { "navigator.controls", E("◀ / ▶ — ВЫБОР • ДЕЙСТВИЕ — МАРШРУТ • ОТМЕНА — ЗАКРЫТЬ", "◀ / ▶ — SELECT • ACTION — ROUTE • CANCEL — CLOSE") },
                { "activity.elite_hint", E("   УДЕРЖИВАЙ РУЧНИК + ДЕЙСТВИЕ — ЭЛИТА", "   HOLD HANDBRAKE + ACTION — ELITE") },
                { "activity.premium_hint", E("   УДЕРЖИВАЙ РУЧНИК + ДЕЙСТВИЕ — ПРЕМИУМ", "   HOLD HANDBRAKE + ACTION — PREMIUM") },
                { "activity.start_time", E("{0}   ДЕЙСТВИЕ — НАЧАТЬ   ЗОЛОТО ≤ {1:0}с{2}{3}", "{0}   ACTION — START   GOLD ≤ {1:0}s{2}{3}") },
                { "activity.countdown", E("{0}   СТАРТ ЧЕРЕЗ {1}   ОТМЕНА", "{0}   START IN {1}   CANCEL") },
                { "activity.checkpoint", E("{0}  ТОЧКА {1}/{2}   {3:0.0}с   {4}   ОТМЕНА", "{0}  POINT {1}/{2}   {3:0.0}s   {4}   CANCEL") },
                { "activity.circuit_status", E("КОЛЬЦО  КРУГ {0}/{1}   ТОЧКА {2}/{3}   КРУГ {4:0.0}с   ОБЩ {5:0.0}с{6}   ОТМЕНА", "CIRCUIT  LAP {0}/{1}   POINT {2}/{3}   LAP {4:0.0}s   TOTAL {5:0.0}s{6}   CANCEL") },
                { "activity.drift_start", E("ДРИФТ-ЗАЕЗД   ДЕЙСТВИЕ — НАЧАТЬ   БРОНЗА {0:N0}   ЛЕГЕНДА {1:N0}{2}", "DRIFT CHALLENGE   ACTION — START   BRONZE {0:N0}   LEGEND {1:N0}{2}") },
                { "activity.drift_status", E("ДРИФТ  {0:N0}   {1}   {2:0.0}с   ОТМЕНА", "DRIFT  {0:N0}   {1}   {2:0.0}s   CANCEL") },
                { "nightclub.start", E("НОЧНОЙ АВТОКЛУБ — {0}   ДЕЙСТВИЕ — НАЧАТЬ", "NIGHT CAR CLUB — {0}   ACTION — START") }
            };

        private static string language = Russian;

        public static string LanguageCode =>
            language;

        public static void SetLanguage(
            string code)
        {
            language =
                NormalizeLanguage(
                    code);
        }

        public static string Text(
            string key)
        {
            if (string.IsNullOrWhiteSpace(
                    key))
            {
                return string.Empty;
            }

            if (MotorCityInput.PreferTouchPrompts &&
                TouchPromptOverrides.TryGetValue(
                    key,
                    out LocalizedEntry touchEntry))
            {
                return language == English
                    ? touchEntry.English
                    : touchEntry.Russian;
            }

            if (!Entries.TryGetValue(
                    key,
                    out LocalizedEntry entry))
            {
                return key;
            }

            return language == English
                ? entry.English
                : entry.Russian;
        }

        public static string Format(
            string key,
            params object[] args)
        {
            string template =
                Text(
                    key);

            if (args == null ||
                args.Length == 0)
            {
                return template;
            }

            return string.Format(
                CultureInfo.CurrentCulture,
                template,
                args);
        }

        private static string NormalizeLanguage(
            string code)
        {
            if (string.IsNullOrWhiteSpace(
                    code))
            {
                return Russian;
            }

            return code.StartsWith(
                    English,
                    StringComparison.OrdinalIgnoreCase)
                ? English
                : Russian;
        }

        private static LocalizedEntry E(
            string russian,
            string english)
        {
            return
                new LocalizedEntry(
                    russian,
                    english);
        }

        private readonly struct LocalizedEntry
        {
            public readonly string Russian;
            public readonly string English;

            public LocalizedEntry(
                string russian,
                string english)
            {
                Russian =
                    russian ?? string.Empty;

                English =
                    english ?? russian ?? string.Empty;
            }
        }
    }
}
