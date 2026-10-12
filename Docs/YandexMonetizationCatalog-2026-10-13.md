# Motor City — Yandex Games purchase catalogue (2026-10-13)

The Unity store uses product IDs, **not hardcoded payment prices**. Add each ID below in Yandex Games Developer Console → In-app purchases with the proposed **YAN** price. The real price and currency label displayed to the player come from `ysdk.payments.getCatalog()` through the existing bridge, not from this document. A missing catalogue item stays unavailable in the released game.

| Product ID | Type | Delivered benefit | Proposed catalogue price |
|---|---|---|---|
| `motor_city_supporter_pack` | Permanent, existing product | 100,000 credits once, DeLorean, Pixie EX skin | 199 YAN (proposal; existing console price NOT changed) |
| `motor_city_credits_25000` | Consumable | 25,000 credits per paid token | 19 YAN |
| `motor_city_credits_100000` | Consumable | 100,000 credits per paid token | 49 YAN |
| `motor_city_credits_500000` | Consumable | 500,000 credits per paid token | 159 YAN |
| `motor_city_no_ads` | Permanent | Disable fullscreen interstitial calls and hide sticky-banner; voluntary rewarded video remains | 99 YAN |
| `motor_city_double_income` | Permanent | 2× earned-credit wallet grants (not credited purchase currency, supporter starter grant or rewarded video) | 149 YAN |

**Console actions are required**: register the five new products and choose their final prices; enable payments and ad monetization; enable sticky banners for desktop and mobile landscape. For the no-ads purchase to work as advertised, in Yandex Games → Advertising → Sticky banners **enable API-controlled banner presentation** so the game can call `showBannerAdv` for nonbuyers and `hideBannerAdv` for purchasers. Confirm both actions on the real platform.

**Ad policy:** platform chooses whether to show fullscreen ads; do not force or overlay them. Only request at authored activity boundaries. Banner placement is controlled by Yandex outside the canvas. Rewarded ads are opt-in. SDK may still display platform-forced startup ads after purchase.

**Payment QA:** retrieve price/icon through SDK catalogue; successful purchase only; multiple same-amount consumables with different tokens; failed/cancelled transactions; crash between local grant and consumption; restored pending tokens on relaunch; same signed-in Yandex account on a second device; no double credits; correct permanent ownership after local data cleared; hide sticky and skip interstitial on no-ads ownership; verify on mobile landscape and desktop.

**Caution:** No product is actually created or repriced by a source-code commit. Product values shown here are proposed YAN amounts that must be entered manually in the Developer Console. Do not submit for moderation before end-to-end payment and cloud-save verification on Yandex Games.

References:
- https://yandex.ru/dev/games/doc/ru/sdk/sdk-purchases
- https://yandex.ru/dev/games/doc/ru/sdk/sdk-adv
- https://yandex.ru/dev/games/doc/ru/requirements/1/13
