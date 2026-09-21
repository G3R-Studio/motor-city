using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.Platform
{
    public sealed class MotorCityPurchaseRuntime :
        MonoBehaviour
    {
        private readonly List<PendingPurchase> pending =
            new();

        public IReadOnlyList<PendingPurchase> Pending =>
            pending;

        public void RefreshPending(
            Action completed = null)
        {
            pending.Clear();

            if (!MotorCityPlatform.SupportsPurchases)
            {
                completed?.Invoke();
                return;
            }

            MotorCityPlatform.LoadPendingPurchases(
                (success, payload) =>
                {
                    if (success)
                    {
                        Parse(
                            payload);
                    }

                    completed?.Invoke();
                });
        }

        public void ConsumeAfterGrant(
            string token,
            Action<bool> completed = null)
        {
            if (string.IsNullOrWhiteSpace(
                    token))
            {
                completed?.Invoke(
                    false);
                return;
            }

            MotorCityPlatform.ConsumePurchase(
                token,
                success =>
                {
                    if (success)
                    {
                        pending.RemoveAll(
                            item =>
                                item.Token ==
                                token);
                    }

                    completed?.Invoke(
                        success);
                });
        }

        private void Parse(
            string payload)
        {
            if (string.IsNullOrWhiteSpace(
                    payload))
            {
                return;
            }

            string[] entries =
                payload.Split(';');

            foreach (string entry in
                     entries)
            {
                int split =
                    entry.IndexOf(':');

                if (split <= 0)
                    continue;

                string productId =
                    Decode(
                        entry.Substring(
                            0,
                            split));

                string token =
                    Decode(
                        entry.Substring(
                            split + 1));

                if (string.IsNullOrWhiteSpace(
                        productId) ||
                    string.IsNullOrWhiteSpace(
                        token))
                {
                    continue;
                }

                pending.Add(
                    new PendingPurchase(
                        productId,
                        token));
            }
        }

        private static string Decode(
            string value)
        {
            return
                string.IsNullOrEmpty(
                    value)
                    ? string.Empty
                    : Uri.UnescapeDataString(
                        value.Replace(
                            "+",
                            " "));
        }
    }

    public readonly struct PendingPurchase
    {
        public readonly string ProductId;
        public readonly string Token;

        public PendingPurchase(
            string productId,
            string token)
        {
            ProductId =
                productId;
            Token =
                token;
        }
    }
}
