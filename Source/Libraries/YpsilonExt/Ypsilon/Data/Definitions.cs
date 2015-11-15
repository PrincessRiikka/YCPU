﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Ypsilon.Data
{
    static class Definitions
    {
        private static Dictionary<string, ASpobDefinition> m_Spobs = new Dictionary<string, ASpobDefinition>();

        public static ASpobDefinition GetSpob(string key)
        {
            key = key.ToLower();

            ASpobDefinition spob;
            if (m_Spobs.TryGetValue(key, out spob))
                return spob;

            return null;
        }

        public static void AddSpob(string key, ASpobDefinition spob)
        {
            key = key.ToLower();

            if (m_Spobs.ContainsKey(key))
                throw new Exception("Cannot add spob with duplicate key '" + key + "'.");

            m_Spobs.Add(key, spob);
        }

        static Definitions()
        {
            AddSpob("Planet", new ASpobDefinition()
            {
                Color = Color.CornflowerBlue,
                Size = 10f,
                VertexRandomizationFactor = 0f
            });

            AddSpob("Asteroid Small", new ASpobDefinition()
            {
                Color = Color.LightGray,
                Size = 0.5f,
                VertexRandomizationFactor = 0.6f
            });

            AddSpob("Asteroid Large", new ASpobDefinition()
            {
                Color = Color.Gray,
                Size = 2f,
                VertexRandomizationFactor = 0.4f
            });
        }
    }
}
