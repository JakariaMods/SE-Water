//HEAVILY cut down version of FastNoiseLite to get past KSH mod profiler

// MIT License
//
// Copyright(c) 2020 Jordan Peck (jordan.me2@gmail.com)
// Copyright(c) 2020 Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// .'',;:cldxkO00KKXXNNWWWNNXKOkxdollcc::::::;:::ccllloooolllllllllooollc:,'...        ...........',;cldxkO000Okxdlc::;;;,,;;;::cclllllll
// ..',;:ldxO0KXXNNNNNNNNXXK0kxdolcc::::::;;;,,,,,,;;;;;;;;;;:::cclllllc:;'....       ...........',;:ldxO0KXXXK0Okxdolc::;;;;::cllodddddo
// ...',:loxO0KXNNNNNXXKK0Okxdolc::;::::::::;;;,,'''''.....''',;:clllllc:;,'............''''''''',;:loxO0KXNNNNNXK0Okxdollccccllodxxxxxxd
// ....';:ldkO0KXXXKK00Okxdolcc:;;;;;::cclllcc:;;,''..... ....',;clooddolcc:;;;;,,;;;;;::::;;;;;;:cloxk0KXNWWWWWWNXKK0Okxddoooddxxkkkkkxx
// .....';:ldxkOOOOOkxxdolcc:;;;,,,;;:cllooooolcc:;'...      ..,:codxkkkxddooollloooooooollcc:::::clodkO0KXNWWWWWWNNXK00Okxxxxxxxxkkkkxxx
// . ....';:cloddddo___________,,,,;;:clooddddoolc:,...      ..,:ldx__00OOOkkk___kkkkkkxxdollc::::cclodkO0KXXNNNNNNXXK0OOkxxxxxxxxxxxxddd
// .......',;:cccc:|           |,,,;;:cclooddddoll:;'..     ..';cox|  \KKK000|   |KK00OOkxdocc___;::clldxxkO0KKKKK00Okkxdddddddddddddddoo
// .......'',,,,,''|   ________|',,;;::cclloooooolc:;'......___:ldk|   \KK000|   |XKKK0Okxolc|   |;;::cclodxxkkkkxxdoolllcclllooodddooooo
// ''......''''....|   |  ....'',,,,;;;::cclloooollc:;,''.'|   |oxk|    \OOO0|   |KKK00Oxdoll|___|;;;;;::ccllllllcc::;;,,;;;:cclloooooooo
// ;;,''.......... |   |_____',,;;;____:___cllo________.___|   |___|     \xkk|   |KK_______ool___:::;________;;;_______...'',;;:ccclllloo
// c:;,''......... |         |:::/     '   |lo/        |           |      \dx|   |0/       \d|   |cc/        |'/       \......',,;;:ccllo
// ol:;,'..........|    _____|ll/    __    |o/   ______|____    ___|   |   \o|   |/   ___   \|   |o/   ______|/   ___   \ .......'',;:clo
// dlc;,...........|   |::clooo|    /  |   |x\___   \KXKKK0|   |dol|   |\   \|   |   |   |   |   |d\___   \..|   |  /   /       ....',:cl
// xoc;'...  .....'|   |llodddd|    \__|   |_____\   \KKK0O|   |lc:|   |'\       |   |___|   |   |_____\   \.|   |_/___/...      ...',;:c
// dlc;'... ....',;|   |oddddddo\          |          |Okkx|   |::;|   |..\      |\         /|   |          | \         |...    ....',;:c
// ol:,'.......',:c|___|xxxddollc\_____,___|_________/ddoll|___|,,,|___|...\_____|:\ ______/l|___|_________/...\________|'........',;::cc
// c:;'.......';:codxxkkkkxxolc::;::clodxkOO0OOkkxdollc::;;,,''''',,,,''''''''''',,'''''',;:loxkkOOkxol:;,'''',,;:ccllcc:;,'''''',;::ccll
// ;,'.......',:codxkOO0OOkxdlc:;,,;;:cldxxkkxxdolc:;;,,''.....'',;;:::;;,,,'''''........,;cldkO0KK0Okdoc::;;::cloodddoolc:;;;;;::ccllooo
// .........',;:lodxOO0000Okdoc:,,',,;:clloddoolc:;,''.......'',;:clooollc:;;,,''.......',:ldkOKXNNXX0Oxdolllloddxxxxxxdolccccccllooodddd
// .    .....';:cldxkO0000Okxol:;,''',,;::cccc:;,,'.......'',;:cldxxkkxxdolc:;;,'.......';coxOKXNWWWNXKOkxddddxxkkkkkkxdoollllooddxxxxkkk
//       ....',;:codxkO000OOxdoc:;,''',,,;;;;,''.......',,;:clodkO00000Okxolc::;,,''..',;:ldxOKXNWWWNNK0OkkkkkkkkkkkxxddooooodxxkOOOOO000
//       ....',;;clodxkkOOOkkdolc:;,,,,,,,,'..........,;:clodxkO0KKXKK0Okxdolcc::;;,,,;;:codkO0XXNNNNXKK0OOOOOkkkkxxdoollloodxkO0KKKXXXXX
//
// VERSION: 1.0.1
// https://github.com/Auburn/FastNoise

using System;
using System.Runtime.CompilerServices;
using VRageMath;

public class FastNoiseLite
{
    private double mFrequency = 0.01;

    /// <summary>
    /// Create new FastNoise object
    /// </summary>
    public FastNoiseLite()
    {
    }

    private static readonly double[] Gradients2D =
    {
         0.130526192220052f,  0.99144486137381f,   0.38268343236509f,   0.923879532511287f,  0.608761429008721f,  0.793353340291235f,  0.793353340291235f,  0.608761429008721f,
         0.923879532511287f,  0.38268343236509f,   0.99144486137381f,   0.130526192220051f,  0.99144486137381f,  -0.130526192220051f,  0.923879532511287f, -0.38268343236509f,
         0.793353340291235f, -0.60876142900872f,   0.608761429008721f, -0.793353340291235f,  0.38268343236509f,  -0.923879532511287f,  0.130526192220052f, -0.99144486137381f,
        -0.130526192220052f, -0.99144486137381f,  -0.38268343236509f,  -0.923879532511287f, -0.608761429008721f, -0.793353340291235f, -0.793353340291235f, -0.608761429008721f,
        -0.923879532511287f, -0.38268343236509f,  -0.99144486137381f,  -0.130526192220052f, -0.99144486137381f,   0.130526192220051f, -0.923879532511287f,  0.38268343236509f,
        -0.793353340291235f,  0.608761429008721f, -0.608761429008721f,  0.793353340291235f, -0.38268343236509f,   0.923879532511287f, -0.130526192220052f,  0.99144486137381f,
         0.130526192220052f,  0.99144486137381f,   0.38268343236509f,   0.923879532511287f,  0.608761429008721f,  0.793353340291235f,  0.793353340291235f,  0.608761429008721f,
         0.923879532511287f,  0.38268343236509f,   0.99144486137381f,   0.130526192220051f,  0.99144486137381f,  -0.130526192220051f,  0.923879532511287f, -0.38268343236509f,
         0.793353340291235f, -0.60876142900872f,   0.608761429008721f, -0.793353340291235f,  0.38268343236509f,  -0.923879532511287f,  0.130526192220052f, -0.99144486137381f,
        -0.130526192220052f, -0.99144486137381f,  -0.38268343236509f,  -0.923879532511287f, -0.608761429008721f, -0.793353340291235f, -0.793353340291235f, -0.608761429008721f,
        -0.923879532511287f, -0.38268343236509f,  -0.99144486137381f,  -0.130526192220052f, -0.99144486137381f,   0.130526192220051f, -0.923879532511287f,  0.38268343236509f,
        -0.793353340291235f,  0.608761429008721f, -0.608761429008721f,  0.793353340291235f, -0.38268343236509f,   0.923879532511287f, -0.130526192220052f,  0.99144486137381f,
         0.130526192220052f,  0.99144486137381f,   0.38268343236509f,   0.923879532511287f,  0.608761429008721f,  0.793353340291235f,  0.793353340291235f,  0.608761429008721f,
         0.923879532511287f,  0.38268343236509f,   0.99144486137381f,   0.130526192220051f,  0.99144486137381f,  -0.130526192220051f,  0.923879532511287f, -0.38268343236509f,
         0.793353340291235f, -0.60876142900872f,   0.608761429008721f, -0.793353340291235f,  0.38268343236509f,  -0.923879532511287f,  0.130526192220052f, -0.99144486137381f,
        -0.130526192220052f, -0.99144486137381f,  -0.38268343236509f,  -0.923879532511287f, -0.608761429008721f, -0.793353340291235f, -0.793353340291235f, -0.608761429008721f,
        -0.923879532511287f, -0.38268343236509f,  -0.99144486137381f,  -0.130526192220052f, -0.99144486137381f,   0.130526192220051f, -0.923879532511287f,  0.38268343236509f,
        -0.793353340291235f,  0.608761429008721f, -0.608761429008721f,  0.793353340291235f, -0.38268343236509f,   0.923879532511287f, -0.130526192220052f,  0.99144486137381f,
         0.130526192220052f,  0.99144486137381f,   0.38268343236509f,   0.923879532511287f,  0.608761429008721f,  0.793353340291235f,  0.793353340291235f,  0.608761429008721f,
         0.923879532511287f,  0.38268343236509f,   0.99144486137381f,   0.130526192220051f,  0.99144486137381f,  -0.130526192220051f,  0.923879532511287f, -0.38268343236509f,
         0.793353340291235f, -0.60876142900872f,   0.608761429008721f, -0.793353340291235f,  0.38268343236509f,  -0.923879532511287f,  0.130526192220052f, -0.99144486137381f,
        -0.130526192220052f, -0.99144486137381f,  -0.38268343236509f,  -0.923879532511287f, -0.608761429008721f, -0.793353340291235f, -0.793353340291235f, -0.608761429008721f,
        -0.923879532511287f, -0.38268343236509f,  -0.99144486137381f,  -0.130526192220052f, -0.99144486137381f,   0.130526192220051f, -0.923879532511287f,  0.38268343236509f,
        -0.793353340291235f,  0.608761429008721f, -0.608761429008721f,  0.793353340291235f, -0.38268343236509f,   0.923879532511287f, -0.130526192220052f,  0.99144486137381f,
         0.130526192220052f,  0.99144486137381f,   0.38268343236509f,   0.923879532511287f,  0.608761429008721f,  0.793353340291235f,  0.793353340291235f,  0.608761429008721f,
         0.923879532511287f,  0.38268343236509f,   0.99144486137381f,   0.130526192220051f,  0.99144486137381f,  -0.130526192220051f,  0.923879532511287f, -0.38268343236509f,
         0.793353340291235f, -0.60876142900872f,   0.608761429008721f, -0.793353340291235f,  0.38268343236509f,  -0.923879532511287f,  0.130526192220052f, -0.99144486137381f,
        -0.130526192220052f, -0.99144486137381f,  -0.38268343236509f,  -0.923879532511287f, -0.608761429008721f, -0.793353340291235f, -0.793353340291235f, -0.608761429008721f,
        -0.923879532511287f, -0.38268343236509f,  -0.99144486137381f,  -0.130526192220052f, -0.99144486137381f,   0.130526192220051f, -0.923879532511287f,  0.38268343236509f,
        -0.793353340291235f,  0.608761429008721f, -0.608761429008721f,  0.793353340291235f, -0.38268343236509f,   0.923879532511287f, -0.130526192220052f,  0.99144486137381f,
         0.38268343236509f,   0.923879532511287f,  0.923879532511287f,  0.38268343236509f,   0.923879532511287f, -0.38268343236509f,   0.38268343236509f,  -0.923879532511287f,
        -0.38268343236509f,  -0.923879532511287f, -0.923879532511287f, -0.38268343236509f,  -0.923879532511287f,  0.38268343236509f,  -0.38268343236509f,   0.923879532511287f,
    };

    private static readonly double[] Gradients3D =
    {
        0, 1, 1, 0,  0,-1, 1, 0,  0, 1,-1, 0,  0,-1,-1, 0,
        1, 0, 1, 0, -1, 0, 1, 0,  1, 0,-1, 0, -1, 0,-1, 0,
        1, 1, 0, 0, -1, 1, 0, 0,  1,-1, 0, 0, -1,-1, 0, 0,
        0, 1, 1, 0,  0,-1, 1, 0,  0, 1,-1, 0,  0,-1,-1, 0,
        1, 0, 1, 0, -1, 0, 1, 0,  1, 0,-1, 0, -1, 0,-1, 0,
        1, 1, 0, 0, -1, 1, 0, 0,  1,-1, 0, 0, -1,-1, 0, 0,
        0, 1, 1, 0,  0,-1, 1, 0,  0, 1,-1, 0,  0,-1,-1, 0,
        1, 0, 1, 0, -1, 0, 1, 0,  1, 0,-1, 0, -1, 0,-1, 0,
        1, 1, 0, 0, -1, 1, 0, 0,  1,-1, 0, 0, -1,-1, 0, 0,
        0, 1, 1, 0,  0,-1, 1, 0,  0, 1,-1, 0,  0,-1,-1, 0,
        1, 0, 1, 0, -1, 0, 1, 0,  1, 0,-1, 0, -1, 0,-1, 0,
        1, 1, 0, 0, -1, 1, 0, 0,  1,-1, 0, 0, -1,-1, 0, 0,
        0, 1, 1, 0,  0,-1, 1, 0,  0, 1,-1, 0,  0,-1,-1, 0,
        1, 0, 1, 0, -1, 0, 1, 0,  1, 0,-1, 0, -1, 0,-1, 0,
        1, 1, 0, 0, -1, 1, 0, 0,  1,-1, 0, 0, -1,-1, 0, 0,
        1, 1, 0, 0,  0,-1, 1, 0, -1, 1, 0, 0,  0,-1,-1, 0
    };

    // Hashing
    private const int PrimeX = 501125321;
    private const int PrimeY = 1136930381;
    private const int PrimeZ = 1720413743;

    /// <returns>
    /// Noise output bounded between -1...1
    /// </returns>
    public double GetNoise(double x, double y)
    {
        x *= mFrequency;
        y *= mFrequency;

        int x0 = x >= 0 ? (int)x : (int)x - 1;
        int y0 = y >= 0 ? (int)y : (int)y - 1;

        double xd0 = (x - x0);
        double yd0 = (y - y0);
        double xd1 = xd0 - 1;
        double yd1 = yd0 - 1;


        double xs = xd0 * xd0 * xd0 * (xd0 * (xd0 * 6 - 15) + 10);
        double ys = yd0 * yd0 * yd0 * (yd0 * (yd0 * 6 - 15) + 10);

        x0 *= PrimeX;
        y0 *= PrimeY;
        int x1 = x0 + PrimeX;
        int y1 = y0 + PrimeY;

        int hash = ((x0 ^ y1) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 127 << 1;

        double xf0 = xd0 * Gradients2D[hash] + yd0 * Gradients2D[hash | 1];

        hash = ((x1 ^ y0) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 127 << 1;

        xf0 = MathHelper.Lerp(xf0, xd1 * Gradients2D[hash] + yd0 * Gradients2D[hash | 1], xs);

        hash = ((x0 ^ y1) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 127 << 1;

        double xf1 = xd0 * Gradients2D[hash] + yd1 * Gradients2D[hash | 1];

        hash = ((x1 ^ y1) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 127 << 1;

        xf1 = MathHelper.Lerp(xf1, xd1 * Gradients2D[hash] + yd1 * Gradients2D[hash | 1], xs);

        return MathHelper.Lerp(xf0, xf1, ys) * 1.4247691104677813f; //This is unimagineably complex just to avoid the KSH Mod Profiler. I don't know what it does but it's dozens of helper methods compressed into one
    }

    /// <summary>
    /// 3D noise at given position using current settings
    /// </summary>
    /// <returns>
    /// Noise output bounded between -1...1
    /// </returns>
    public double GetNoise(double x, double y, double z)
    {
        x *= mFrequency;
        y *= mFrequency;
        z *= mFrequency;

        int x0 = x >= 0 ? (int)x : (int)x - 1;
        int y0 = y >= 0 ? (int)y : (int)y - 1;
        int z0 = z >= 0 ? (int)z : (int)z - 1;

        double xd0 = (x - x0);
        double yd0 = (y - y0);
        double zd0 = (z - z0);
        double xd1 = xd0 - 1;
        double yd1 = yd0 - 1;
        double zd1 = zd0 - 1;

        double xs = xd0 * xd0 * xd0 * (xd0 * (xd0 * 6 - 15) + 10);
        double ys = yd0 * yd0 * yd0 * (yd0 * (yd0 * 6 - 15) + 10);
        double zs = zd0 * zd0 * zd0 * (zd0 * (zd0 * 6 - 15) + 10);

        x0 *= PrimeX;
        y0 *= PrimeY;
        z0 *= PrimeZ;
        int x1 = x0 + PrimeX;
        int y1 = y0 + PrimeY;
        int z1 = z0 + PrimeZ;

        int hash = ((x0 ^ y0 ^ z0) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        double xf00 = xd0 * Gradients3D[hash] + yd0 * Gradients3D[hash | 1] + zd0 * Gradients3D[hash | 2];

        hash = ((x1 ^ y0 ^ z0) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        xf00 = MathHelper.Lerp(xf00, xd1 * Gradients3D[hash] + yd0 * Gradients3D[hash | 1] + zd0 * Gradients3D[hash | 2], xs);

        hash = ((x0 ^ y1 ^ z0) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        double xf10 = xd0 * Gradients3D[hash] + yd1 * Gradients3D[hash | 1] + zd0 * Gradients3D[hash | 2];

        hash = ((x1 ^ y1 ^ z0) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        xf10 = MathHelper.Lerp(xf10, xd1 * Gradients3D[hash] + yd1 * Gradients3D[hash | 1] + zd0 * Gradients3D[hash | 2], xs);

        hash = ((x0 ^ y0 ^ z1) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        double xf01 = xd0 * Gradients3D[hash] + yd0 * Gradients3D[hash | 1] + zd1 * Gradients3D[hash | 2];

        hash = ((x1 ^ y0 ^ z1) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        xf01 = MathHelper.Lerp(xf01, xd1 * Gradients3D[hash] + yd0 * Gradients3D[hash | 1] + zd1 * Gradients3D[hash | 2], xs);

        hash = ((x0 ^ y1 ^ z1) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        double xf11 = xd0 * Gradients3D[hash] + yd1 * Gradients3D[hash | 1] + zd1 * Gradients3D[hash | 2];

        hash = ((x1 ^ y1 ^ z1) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        xf11 = MathHelper.Lerp(xf11, xd1 * Gradients3D[hash] + yd1 * Gradients3D[hash | 1] + zd1 * Gradients3D[hash | 2], xs);

        double yf0 = MathHelper.Lerp(xf00, xf10, ys);
        double yf1 = MathHelper.Lerp(xf01, xf11, ys);

        return MathHelper.Lerp(yf0, yf1, zs) * 0.964921414852142333984375f;
    }

    /// <summary>
    /// 3D noise at given position using current settings
    /// </summary>
    /// <returns>
    /// Noise output bounded between -1...1
    /// </returns>
    public double GetNoise(Vector3D position)
    {
        position.X *= mFrequency;
        position.Y *= mFrequency;
        position.Z *= mFrequency;

        int x0 = position.X >= 0 ? (int)position.X : (int)position.X - 1;
        int y0 = position.Y >= 0 ? (int)position.Y : (int)position.Y - 1;
        int z0 = position.Z >= 0 ? (int)position.Z : (int)position.Z - 1;

        double xd0 = (position.X - x0);
        double yd0 = (position.Y - y0);
        double zd0 = (position.Z - z0);
        double xd1 = xd0 - 1;
        double yd1 = yd0 - 1;
        double zd1 = zd0 - 1;

        double xs = xd0 * xd0 * xd0 * (xd0 * (xd0 * 6 - 15) + 10);
        double ys = yd0 * yd0 * yd0 * (yd0 * (yd0 * 6 - 15) + 10);
        double zs = zd0 * zd0 * zd0 * (zd0 * (zd0 * 6 - 15) + 10);

        x0 *= PrimeX;
        y0 *= PrimeY;
        z0 *= PrimeZ;
        int x1 = x0 + PrimeX;
        int y1 = y0 + PrimeY;
        int z1 = z0 + PrimeZ;

        int hash = ((x0 ^ y0 ^ z0) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        double xf00 = xd0 * Gradients3D[hash] + yd0 * Gradients3D[hash | 1] + zd0 * Gradients3D[hash | 2];

        hash = ((x1 ^ y0 ^ z0) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        xf00 = MathHelper.Lerp(xf00, xd1 * Gradients3D[hash] + yd0 * Gradients3D[hash | 1] + zd0 * Gradients3D[hash | 2], xs);

        hash = ((x0 ^ y1 ^ z0) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        double xf10 = xd0 * Gradients3D[hash] + yd1 * Gradients3D[hash | 1] + zd0 * Gradients3D[hash | 2];

        hash = ((x1 ^ y1 ^ z0) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        xf10 = MathHelper.Lerp(xf10, xd1 * Gradients3D[hash] + yd1 * Gradients3D[hash | 1] + zd0 * Gradients3D[hash | 2], xs);

        hash = ((x0 ^ y0 ^ z1) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        double xf01 = xd0 * Gradients3D[hash] + yd0 * Gradients3D[hash | 1] + zd1 * Gradients3D[hash | 2];

        hash = ((x1 ^ y0 ^ z1) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        xf01 = MathHelper.Lerp(xf01, xd1 * Gradients3D[hash] + yd0 * Gradients3D[hash | 1] + zd1 * Gradients3D[hash | 2], xs);

        hash = ((x0 ^ y1 ^ z1) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        double xf11 = xd0 * Gradients3D[hash] + yd1 * Gradients3D[hash | 1] + zd1 * Gradients3D[hash | 2];

        hash = ((x1 ^ y1 ^ z1) * 0x27d4eb2d);
        hash ^= hash >> 15;
        hash &= 63 << 2;

        xf11 = MathHelper.Lerp(xf11, xd1 * Gradients3D[hash] + yd1 * Gradients3D[hash | 1] + zd1 * Gradients3D[hash | 2], xs);

        double yf0 = MathHelper.Lerp(xf00, xf10, ys);
        double yf1 = MathHelper.Lerp(xf01, xf11, ys);

        return MathHelper.Lerp(yf0, yf1, zs) * 0.964921414852142333984375f;
    }
}