using DBPBusinessCardEditable.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace DBPBusinessCardEditable.Services
{
    /// <summary>
    /// In-memory store keyed by Employee ID.
    /// Public card URLs use a random Token — EmpId never appears in public URLs.
    /// </summary>
    public class CardProfileService
    {
        // Primary store: EmpId → CardProfile
        private readonly ConcurrentDictionary<string, CardProfile> _store
            = new ConcurrentDictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase);

        // Secondary index: Token → EmpId (for fast public URL lookups)
        private readonly ConcurrentDictionary<string, string> _tokenIndex
            = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public CardProfile Get(string empId)
        {
            if (string.IsNullOrWhiteSpace(empId)) return null;
            _store.TryGetValue(empId.Trim(), out var profile);
            return profile;
        }

        /// <summary>Look up a card by its public token (used in /card/{token} URLs).</summary>
        public CardProfile GetByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            if (!_tokenIndex.TryGetValue(token.Trim(), out var empId)) return null;
            return Get(empId);
        }

        public CardProfile GetOrCreate(string empId)
        {
            return _store.GetOrAdd(empId.Trim(), id => new CardProfile
            {
                EmpId = id,
                Token = GenerateToken()
            });
        }

        public void Save(CardProfile profile)
        {
            profile.EmpId = profile.EmpId.Trim();
            profile.LastUpdated = DateTime.UtcNow;

            // Keep existing token if card already exists, otherwise generate new one
            if (_store.TryGetValue(profile.EmpId, out var existing) && !string.IsNullOrEmpty(existing.Token))
                profile.Token = existing.Token;
            else if (string.IsNullOrEmpty(profile.Token))
                profile.Token = GenerateToken();

            _store[profile.EmpId] = profile;
            _tokenIndex[profile.Token] = profile.EmpId;
        }

        public void Reset(string empId)
        {
            // Keep the same token on reset so QR codes remain valid
            string existingToken = null;
            if (_store.TryGetValue(empId.Trim(), out var existing))
                existingToken = existing.Token;

            var blank = new CardProfile
            {
                EmpId  = empId.Trim(),
                Token  = existingToken ?? GenerateToken()
            };
            _store[blank.EmpId] = blank;
            _tokenIndex[blank.Token] = blank.EmpId;
        }

        public void Delete(string empId)
        {
            if (_store.TryRemove(empId.Trim(), out var profile) && !string.IsNullOrEmpty(profile.Token))
                _tokenIndex.TryRemove(profile.Token, out _);
        }

        public int ClearAll()
        {
            int count = _store.Count;
            _store.Clear();
            _tokenIndex.Clear();
            return count;
        }

        public int Count() => _store.Count;

        public List<CardProfile> GetAll()
            => _store.Values.OrderByDescending(p => p.LastUpdated).ToList();

        // ── Helpers ──────────────────────────────────────────
        /// <summary>Generate a cryptographically random 12-char token.</summary>
        private static string GenerateToken()
        {
            var bytes = new byte[9]; // 9 bytes → 12 base64url chars
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "a").Replace("/", "b").Replace("=", "");
        }
    }
}
