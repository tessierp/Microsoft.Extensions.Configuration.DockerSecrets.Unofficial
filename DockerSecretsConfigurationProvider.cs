

namespace Microsoft.Extensions.Configuration.DockerSecrets
{
    using FileProviders;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// An docker secrets based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class DockerSecretsConfigurationProvider : ConfigurationProvider
    {
        private readonly DockerSecretsConfigurationSource _source;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="source">The settings.</param>
        public DockerSecretsConfigurationProvider(DockerSecretsConfigurationSource source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Loads the docker secrets.
        /// </summary>
        public override void Load()
        {
            Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (_source.FileProvider == null)
            {
                // A docker secrets folder file provider was not supplied, so lets get one
                if (Directory.Exists(_source.SecretsDirectory))
                {
                    _source.FileProvider = new PhysicalFileProvider(_source.SecretsDirectory);
                    return;
                }

                // We were not able to get a docker secrets folder file provider, if optional just return
                if (_source.Optional)
                {
                    return;
                }

                // No docker secrets folder file provider, return an exception
                throw new DirectoryNotFoundException("DockerSecrets directory doesn't exist and is not optional.");
            }

            var secretsDir = _source.FileProvider.GetDirectoryContents("/");

            // So we have nothing in the docker secrets folder and it is optional.
            // We have nothing else to do
            if (!secretsDir.Exists && _source.Optional)
            {
                return;
            }

            // Check if there is content in the docker secrets folder, if nothing is in there, we have a problem if it isn't optional
            if (!secretsDir.Exists && !_source.Optional)
            {
                throw new DirectoryNotFoundException("DockerSecrets directory doesn't exist and is not optional.");
            }

            // Process the docker folder's secrets
            foreach (var file in secretsDir)
            {
                if (file.IsDirectory)
                {
                    continue;
                }

                using (var stream = file.CreateReadStream())
                using (var streamReader = new StreamReader(stream))
                {
                    if (_source.IgnoreCondition == null || !_source.IgnoreCondition(file.Name))
                    {
                        Data.Add(NormalizeKey(file.Name), streamReader.ReadToEnd());
                    }
                }
            }
        }

        /// <summary>
        /// Normalizes the key. Basically all this does is, takes the docker secrets key and changes it so that it is
        /// understandable to the asp.net core / net core app using Microsoft's json configuration
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Normalized key.</returns>
        private string NormalizeKey(string key)
        {
            if (key.Contains(_source.DockerSecretsWordSeparator))
            {
                return key.Replace(_source.DockerSecretsWordSeparator, ConfigurationPath.KeyDelimiter);
            }

            return key;
        }
    }
}
