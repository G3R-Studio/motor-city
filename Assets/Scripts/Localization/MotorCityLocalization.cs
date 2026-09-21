using System;
using System.Collections.Generic;
using System.Globalization;

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
                { "hud.underground", E("ПОДПОЛЬЕ", "NIGHT CLUB") },

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
                { "vehicle.next_available", E("   •   ДАЛЬШЕ: {0} — ДОСТУПНА", "   •   NEXT: {0} — AVAILABLE") },
                { "vehicle.next_rep", E("   •   ДАЛЬШЕ: {0} — {1:N0} РЕП", "   •   NEXT: {0} — {1:N0} REP") },
                { "vehicle.garage_line", E("МАШИНА {0}/{1}: {2}   •   МАСТЕРСТВО {3}/10{4}", "CAR {0}/{1}: {2}   •   MASTERY {3}/10{4}") },
                { "vehicle.mastery_max", E("МАСТЕРСТВО: УР. 10/10   •   {0:N0} ОПЫТ   •   МАКСИМУМ", "MASTERY: LVL 10/10   •   {0:N0} XP   •   MAX") },
                { "vehicle.mastery", E("МАСТЕРСТВО: УР. {0}/10   •   {1:N0}/{2:N0} ОПЫТ", "MASTERY: LVL {0}/10   •   {1:N0}/{2:N0} XP") },
                { "vehicle.mastery_short", E("МАСТ {0}/10", "MAST {0}/10") },
                { "vehicle.stats", E("БАЗА: СКОРОСТЬ {0}   •   РАЗГОН {1}   •   СЦЕП {2}%   •   СТАБ {3}   •   {4}", "BASE: SPEED {0}   •   ACCEL {1}   •   GRIP {2}%   •   STAB {3}   •   {4}") },

                { "garage.marker", E("Фиолетовый маркер: гараж", "Purple marker: garage") },
                { "garage.opened", E("ГАРАЖ ОТКРЫТ", "GARAGE OPEN") },
                { "garage.prompt", E("ГАРАЖ — нажми E", "GARAGE — press E") },
                { "garage.vehicles_unavailable", E("МАШИНЫ НЕДОСТУПНЫ", "VEHICLES UNAVAILABLE") },
                { "garage.max_level", E("МАКС", "MAX") },
                { "garage.level", E("УР. {0}/{1}", "LVL {0}/{1}") },
                { "garage.need_credits", E("Для «{0}» нужно {1:N0} КР", "“{0}” needs {1:N0} CR") },
                { "garage.already_max", E("{0} уже улучшен до максимума", "{0} is already maxed") },
                { "garage.upgraded", E("{0} улучшен до уровня {1}", "{0} upgraded to level {1}") },
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
                { "risk.reason_level", E("{0}   •   ВНИМАНИЕ {1}/5", "{0}   •   ATTENTION {1}/5") }
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
