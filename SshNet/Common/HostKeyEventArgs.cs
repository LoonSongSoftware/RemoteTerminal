﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet.Security.Cryptography;
using Renci.SshNet.Security;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;

namespace Renci.SshNet.Common
{
    /// <summary>
    /// Provides data for the HostKeyReceived event.
    /// </summary>
    public class HostKeyEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether host key can be trusted.
        /// </summary>
        /// <value>
        ///   <c>true</c> if host key can be trusted; otherwise, <c>false</c>.
        /// </value>
        public bool CanTrust { get; set; }

        /// <summary>
        /// Gets the host key.
        /// </summary>
        public byte[] HostKey { get; private set; }

        /// <summary>
        /// Gets the finger print.
        /// </summary>
        public byte[] FingerPrint { get; private set; }

        /// <summary>
        /// Gets the length of the key in bits.
        /// </summary>
        /// <value>
        /// The length of the key in bits.
        /// </value>
        public int KeyLength { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostKeyEventArgs"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        public HostKeyEventArgs(KeyHostAlgorithm host)
        {
            this.CanTrust = true;   //  Set default value

            this.HostKey = host.Data;

            this.KeyLength = host.Key.KeyLength;

            var md5 = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            byte[] buffer;
            CryptographicBuffer.CopyToByteArray(md5.HashData(CryptographicBuffer.CreateFromByteArray(host.Data)), out buffer);
            this.FingerPrint = buffer;
        }
    }
}
