using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Globalization; // 👈 Yeni eklenen
using System.Text.Json; // 👈 JSON için daha güvenilir bir yöntem kullanmak üzere

namespace FinanceTracker.API.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly HttpClient _http;

        public CurrencyService(HttpClient http)
        {
            _http = http;
        }

        public async Task<decimal> GetRateAsync(string from, string to)
        {
            if (from == "TRY" && to == "TRY")
                return 1m;

            if (to != "TRY")
                throw new Exception("Bu servis şimdilik sadece TRY dönüşümü destekliyor.");

            // Decimal değerleri nokta ondalık ayıracı ile parse etmek için InvariantCulture kullanıyoruz.
            var invariantCulture = CultureInfo.InvariantCulture;

            // 1️⃣ TCMB XML
            try
            {
                string url = "https://www.tcmb.gov.tr/kurlar/today.xml";
                var xmlStr = await _http.GetStringAsync(url);
                var doc = XDocument.Parse(xmlStr);

                var node = doc
                    .Descendants("Currency")
                    .FirstOrDefault(x => (string)x.Attribute("Kod") == from);

                if (node != null)
                {
                    // Alış (ForexBuying) veya Satış (ForexSelling) kurunu çekin.
                    // Genellikle alım satım işlemleri için 'ForexSelling' (Döviz Satış) kullanılır.
                    string rawRate = node.Element("ForexSelling")?.Value;

                    if (string.IsNullOrWhiteSpace(rawRate))
                    {
                        // ForexSelling boşsa (örneğin AUD/CAD gibi çapraz kurlar için olabilir), 
                        // efektif satış (BanknoteSelling) deneyebilirsiniz, ama Foreks genellikle önceliklidir.
                        rawRate = node.Element("BanknoteSelling")?.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(rawRate))
                    {
                        // TCMB XML'de ondalık ayıracı NOKTADIR. 
                        // Bu nedenle Replace işlemi yapmaya GEREK YOKTUR ve InvariantCulture kullanmalıyız.
                        if (decimal.TryParse(rawRate, NumberStyles.Any, invariantCulture, out decimal rate))
                        {
                            // Kurun birimini (Unit) kontrol edin. TCMB'de bazı kurlar (örneğin JPY) 100 birim üstünden verilebilir.
                            string unit = node.Element("Unit")?.Value ?? "1";
                            if (int.TryParse(unit, out int unitValue) && unitValue > 1)
                            {
                                return rate / unitValue; // 100 birimlik kur için 1 birime çevir
                            }
                            return rate;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TCMB XML hata: " + ex.Message);
            }

            // 2️⃣ Genel Para JSON (Daha güvenilir JSON okuma)
            try
            {
                string urlJson = "https://api.genelpara.com/embed/para-birimleri.json";
                var json = await _http.GetStringAsync(urlJson);

                var lookup = from.ToUpper();

                // System.Text.Json kütüphanesini kullanarak daha güvenli bir okuma yapıyoruz.
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    if (document.RootElement.TryGetProperty(lookup, out JsonElement currencyElement))
                    {
                        if (currencyElement.TryGetProperty("satis", out JsonElement satisElement))
                        {
                            string rawRate = satisElement.GetString();
                            if (!string.IsNullOrWhiteSpace(rawRate))
                            {
                                // Genelpara JSON'daki kur değerinde de ondalık ayıracı NOKTADIR.
                                // Bu nedenle Replace işlemi yapmaya GEREK YOKTUR ve InvariantCulture kullanmalıyız.
                                if (decimal.TryParse(rawRate, NumberStyles.Any, invariantCulture, out decimal rate))
                                {
                                    return rate;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Genel Para JSON hata: " + ex.Message);
            }

            // 3️⃣ Google Finance Scraper (son çare)
            try
            {
                string googleUrl = $"https://www.google.com/finance/quote/{from}-TRY";
                var html = await _http.GetStringAsync(googleUrl);

                var match = Regex.Match(html, @"data-last-price=""(?<v>[\d.]+)""");

                if (match.Success)
                {
                    string rawRate = match.Groups["v"].Value;
                    // Google'dan gelen değerde de ondalık ayıracı NOKTADIR.
                    if (decimal.TryParse(rawRate, NumberStyles.Any, invariantCulture, out decimal rate))
                    {
                        return rate;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Google Finance hata: " + ex.Message);
            }

            throw new Exception("Kur alınamadı: " + from);
        }
    }
}