﻿namespace Imgeneus.Core.Structures.Configuration
{
    public sealed class WorldConfiguration : BaseConfiguration
    {
        private readonly int defaultPort = 30810;
        private readonly string defaultName = "Imgeneus";
        private readonly int defaultMaxNumberOfConnections = 1000;

        /// <summary>
        /// Gets the maximum number of connections allowed on the server.
        /// </summary>
        public int MaximumNumberOfConnections { get; set; }

        /// <summary>
        /// Gets or sets the world's id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the world's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Inter-Server configuration
        /// </summary>
        public InterServerConfiguration InterServerConfiguration { get; set; }

        public WorldConfiguration()
        {
            this.Port = defaultPort;
            this.Name = defaultName;
            this.MaximumNumberOfConnections = defaultMaxNumberOfConnections;
            InterServerConfiguration = new InterServerConfiguration();
        }
    }
}