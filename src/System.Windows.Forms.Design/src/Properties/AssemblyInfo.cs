﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

[assembly: System.Runtime.InteropServices.ComVisible(false)]

// expose internal types to System.Design for type forwarding
[assembly: InternalsVisibleTo("System.Design, PublicKey=002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293")]

[assembly: InternalsVisibleTo("System.Windows.Forms.Design.Tests, PublicKey=00000000000000000400000000000000")]
[assembly: InternalsVisibleTo("System.Windows.Forms.UI.IntegrationTests, PublicKey=00000000000000000400000000000000")]
