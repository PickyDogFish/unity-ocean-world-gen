//
// vec3  psrdnoise(vec2 pos, vec2 per, float rot)
// vec3  psdnoise(vec2 pos, vec2 per)
// float psrnoise(vec2 pos, vec2 per, float rot)
// float psnoise(vec2 pos, vec2 per)
// vec3  srdnoise(vec2 pos, float rot)
// vec3  sdnoise(vec2 pos)
// float srnoise(vec2 pos, float rot)
// float snoise(vec2 pos)
//
// Periodic (tiling) 2-D simplex noise (hexagonal lattice gradient noise)
// with rotating gradients and analytic derivatives.
// Variants also without the derivative (no "d" in the name), without
// the tiling property (no "p" in the name) and without the rotating
// gradients (no "r" in the name).
//
// This is (yet) another variation on simplex noise. It's similar to the
// version presented by Ken Perlin, but the grid is axis-aligned and
// slightly stretched in the y direction to permit rectangular tiling.
//
// The noise can be made to tile seamlessly to any integer period in x and
// any even integer period in y. Odd periods may be specified for y, but
// then the actual tiling period will be twice that number.
//
// The rotating gradients give the appearance of a swirling motion, and can
// serve a similar purpose for animation as motion along z in 3-D noise.
// The rotating gradients in conjunction with the analytic derivatives
// can make "flow noise" effects as presented by Perlin and Neyret.
//
// vec3 {p}s{r}dnoise(vec2 pos {, vec2 per} {, float rot})
// "pos" is the input (x,y) coordinate
// "per" is the x and y period, where per.x is a positive integer
//    and per.y is a positive even integer
// "rot" is the angle to rotate the gradients (any float value,
//    where 0.0 is no rotation and 1.0 is one full turn)
// The first component of the 3-element return vector is the noise value.
// The second and third components are the x and y partial derivatives.
//
// float {p}s{r}noise(vec2 pos {, vec2 per} {, float rot})
// "pos" is the input (x,y) coordinate
// "per" is the x and y period, where per.x is a positive integer
//    and per.y is a positive even integer
// "rot" is the angle to rotate the gradients (any float value,
//    where 0.0 is no rotation and 1.0 is one full turn)
// The return value is the noise value.
// Partial derivatives are not computed, making these functions faster.
//
// Author: Stefan Gustavson (stefan.gustavson@gmail.com)
// Version 2016-05-10.
//
// Many thanks to Ian McEwan of Ashima Arts for the
// idea of using a permutation polynomial.
//
// Copyright (c) 2016 Stefan Gustavson. All rights reserved.
// Distributed under the MIT license. See LICENSE file.
// https://github.com/stegu/webgl-noise
//

//
// TODO: One-pixel wide artefacts used to occur due to precision issues with
// the gradient indexing. This is specific to this variant of noise, because
// one axis of the simplex grid is perfectly aligned with the input x axis.
// The errors were rare, and they are now very unlikely to ever be visible
// after a quick fix was introduced: a small offset is added to the y coordinate.
// A proper fix would involve using round() instead of floor() in selected
// places, but the quick fix works fine.
// (If you run into problems with this, please let me know.)
//




#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))

float3 mod289(float3 x)
{
    return x-floor(x*(1./289.))*289.;
}

float3 permute(float3 x)
{
    return mod289((x*34.+10.)*x);
}

float2 rgrad2(float2 p, float rot)
{
#if 0
    float u = permute(permute(p.x)+p.y)*0.024390243+rot;
    u = 4.*frac(u)-2.;
    return float2(abs(u)-1., abs(abs(u+1.)-2.)-1.);
#else
    float u = permute(permute(p.x)+p.y)*0.024390243+rot;
    u = frac(u)*6.2831855;
    return float2(cos(u), sin(u));
#endif
}

float3 psrdnoise(float2 pos, float2 per, float rot)
{
    pos.y += 0.01;
    float2 uv = float2(pos.x+pos.y*0.5, pos.y);
    float2 i0 = floor(uv);
    float2 f0 = frac(uv);
    float2 i1 = f0.x>f0.y ? float2(1., 0.) : float2(0., 1.);
    float2 p0 = float2(i0.x-i0.y*0.5, i0.y);
    float2 p1 = float2(p0.x+i1.x-i1.y*0.5, p0.y+i1.y);
    float2 p2 = float2(p0.x+0.5, p0.y+1.);
    i1 = i0+i1;
    float2 i2 = i0+float2(1., 1.);
    float2 d0 = pos-p0;
    float2 d1 = pos-p1;
    float2 d2 = pos-p2;
    float3 xw = glsl_mod(float3(p0.x, p1.x, p2.x), per.x);
    float3 yw = glsl_mod(float3(p0.y, p1.y, p2.y), per.y);
    float3 iuw = xw+0.5*yw;
    float3 ivw = yw;
    float2 g0 = rgrad2(float2(iuw.x, ivw.x), rot);
    float2 g1 = rgrad2(float2(iuw.y, ivw.y), rot);
    float2 g2 = rgrad2(float2(iuw.z, ivw.z), rot);
    float3 w = float3(dot(g0, d0), dot(g1, d1), dot(g2, d2));
    float3 t = 0.8-float3(dot(d0, d0), dot(d1, d1), dot(d2, d2));
    float3 dtdx = -2.*float3(d0.x, d1.x, d2.x);
    float3 dtdy = -2.*float3(d0.y, d1.y, d2.y);
    if (t.x<0.)
    {
        dtdx.x = 0.;
        dtdy.x = 0.;
        t.x = 0.;
    }
    
    if (t.y<0.)
    {
        dtdx.y = 0.;
        dtdy.y = 0.;
        t.y = 0.;
    }
    
    if (t.z<0.)
    {
        dtdx.z = 0.;
        dtdy.z = 0.;
        t.z = 0.;
    }
    
    float3 t2 = t*t;
    float3 t4 = t2*t2;
    float3 t3 = t2*t;
    float n = dot(t4, w);
    float2 dt0 = float2(dtdx.x, dtdy.x)*4.*t3.x;
    float2 dn0 = t4.x*g0+dt0*w.x;
    float2 dt1 = float2(dtdx.y, dtdy.y)*4.*t3.y;
    float2 dn1 = t4.y*g1+dt1*w.y;
    float2 dt2 = float2(dtdx.z, dtdy.z)*4.*t3.z;
    float2 dn2 = t4.z*g2+dt2*w.z;
    return 11.*float3(n, dn0+dn1+dn2);
}

float3 psdnoise(float2 pos, float2 per)
{
    return psrdnoise(pos, per, 0.);
}

float psrnoise(float2 pos, float2 per, float rot)
{
    pos.y += 0.001;
    float2 uv = float2(pos.x+pos.y*0.5, pos.y);
    float2 i0 = floor(uv);
    float2 f0 = frac(uv);
    float2 i1 = f0.x>f0.y ? float2(1., 0.) : float2(0., 1.);
    float2 p0 = float2(i0.x-i0.y*0.5, i0.y);
    float2 p1 = float2(p0.x+i1.x-i1.y*0.5, p0.y+i1.y);
    float2 p2 = float2(p0.x+0.5, p0.y+1.);
    i1 = i0+i1;
    float2 i2 = i0+float2(1., 1.);
    float2 d0 = pos-p0;
    float2 d1 = pos-p1;
    float2 d2 = pos-p2;
    float3 xw = glsl_mod(float3(p0.x, p1.x, p2.x), per.x);
    float3 yw = glsl_mod(float3(p0.y, p1.y, p2.y), per.y);
    float3 iuw = xw+0.5*yw;
    float3 ivw = yw;
    float2 g0 = rgrad2(float2(iuw.x, ivw.x), rot);
    float2 g1 = rgrad2(float2(iuw.y, ivw.y), rot);
    float2 g2 = rgrad2(float2(iuw.z, ivw.z), rot);
    float3 w = float3(dot(g0, d0), dot(g1, d1), dot(g2, d2));
    float3 t = 0.8-float3(dot(d0, d0), dot(d1, d1), dot(d2, d2));
    t = max(t, 0.);
    float3 t2 = t*t;
    float3 t4 = t2*t2;
    float n = dot(t4, w);
    return 11.*n;
}

float psnoise(float2 pos, float2 per)
{
    return psrnoise(pos, per, 0.);
}

float3 srdnoise(float2 pos, float rot)
{
    pos.y += 0.001;
    float2 uv = float2(pos.x+pos.y*0.5, pos.y);
    float2 i0 = floor(uv);
    float2 f0 = frac(uv);
    float2 i1 = f0.x>f0.y ? float2(1., 0.) : float2(0., 1.);
    float2 p0 = float2(i0.x-i0.y*0.5, i0.y);
    float2 p1 = float2(p0.x+i1.x-i1.y*0.5, p0.y+i1.y);
    float2 p2 = float2(p0.x+0.5, p0.y+1.);
    i1 = i0+i1;
    float2 i2 = i0+float2(1., 1.);
    float2 d0 = pos-p0;
    float2 d1 = pos-p1;
    float2 d2 = pos-p2;
    float3 x = float3(p0.x, p1.x, p2.x);
    float3 y = float3(p0.y, p1.y, p2.y);
    float3 iuw = x+0.5*y;
    float3 ivw = y;
    iuw = mod289(iuw);
    ivw = mod289(ivw);
    float2 g0 = rgrad2(float2(iuw.x, ivw.x), rot);
    float2 g1 = rgrad2(float2(iuw.y, ivw.y), rot);
    float2 g2 = rgrad2(float2(iuw.z, ivw.z), rot);
    float3 w = float3(dot(g0, d0), dot(g1, d1), dot(g2, d2));
    float3 t = 0.8-float3(dot(d0, d0), dot(d1, d1), dot(d2, d2));
    float3 dtdx = -2.*float3(d0.x, d1.x, d2.x);
    float3 dtdy = -2.*float3(d0.y, d1.y, d2.y);
    if (t.x<0.)
    {
        dtdx.x = 0.;
        dtdy.x = 0.;
        t.x = 0.;
    }
    
    if (t.y<0.)
    {
        dtdx.y = 0.;
        dtdy.y = 0.;
        t.y = 0.;
    }
    
    if (t.z<0.)
    {
        dtdx.z = 0.;
        dtdy.z = 0.;
        t.z = 0.;
    }
    
    float3 t2 = t*t;
    float3 t4 = t2*t2;
    float3 t3 = t2*t;
    float n = dot(t4, w);
    float2 dt0 = float2(dtdx.x, dtdy.x)*4.*t3.x;
    float2 dn0 = t4.x*g0+dt0*w.x;
    float2 dt1 = float2(dtdx.y, dtdy.y)*4.*t3.y;
    float2 dn1 = t4.y*g1+dt1*w.y;
    float2 dt2 = float2(dtdx.z, dtdy.z)*4.*t3.z;
    float2 dn2 = t4.z*g2+dt2*w.z;
    return 11.*float3(n, dn0+dn1+dn2);
}

float3 sdnoise(float2 pos)
{
    return srdnoise(pos, 0.);
}

float srnoise(float2 pos, float rot)
{
    pos.y += 0.001;
    float2 uv = float2(pos.x+pos.y*0.5, pos.y);
    float2 i0 = floor(uv);
    float2 f0 = frac(uv);
    float2 i1 = f0.x>f0.y ? float2(1., 0.) : float2(0., 1.);
    float2 p0 = float2(i0.x-i0.y*0.5, i0.y);
    float2 p1 = float2(p0.x+i1.x-i1.y*0.5, p0.y+i1.y);
    float2 p2 = float2(p0.x+0.5, p0.y+1.);
    i1 = i0+i1;
    float2 i2 = i0+float2(1., 1.);
    float2 d0 = pos-p0;
    float2 d1 = pos-p1;
    float2 d2 = pos-p2;
    float3 x = float3(p0.x, p1.x, p2.x);
    float3 y = float3(p0.y, p1.y, p2.y);
    float3 iuw = x+0.5*y;
    float3 ivw = y;
    iuw = mod289(iuw);
    ivw = mod289(ivw);
    float2 g0 = rgrad2(float2(iuw.x, ivw.x), rot);
    float2 g1 = rgrad2(float2(iuw.y, ivw.y), rot);
    float2 g2 = rgrad2(float2(iuw.z, ivw.z), rot);
    float3 w = float3(dot(g0, d0), dot(g1, d1), dot(g2, d2));
    float3 t = 0.8-float3(dot(d0, d0), dot(d1, d1), dot(d2, d2));
    t = max(t, 0.);
    float3 t2 = t*t;
    float3 t4 = t2*t2;
    float n = dot(t4, w);
    return 11.*n;
}

float snoise(float2 pos)
{
    return srnoise(pos, 0.);
}

float3 sdnoise01(float2 pos){
    float3 nd = sdnoise(pos);
    return  nd;
}











//Unity gradient noise implementation
float2 Unity_GradientNoise_Dir_float(float2 p)
{
    // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
    p = p % 289;
    // need full precision, otherwise half overflows when p > 1
    float x = float(34 * p.x + 1) * p.x % 289 + p.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

float Unity_GradientNoise_float(float2 UV)
{
    float2 p = UV;
    float2 ip = floor(p);
    float2 fp = frac(p);
    float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
    float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
    float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
    float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
    fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
    return (lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5) * 0.95;
}




//Unity value noise implementation
float unity_noise_randomValue (float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
}

float unity_noise_interpolate (float a, float b, float t)
{
    return (1.0-t)*a + (t*b);
}

float unity_valueNoise (float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);

    uv = abs(frac(uv) - 0.5);
    float2 c0 = i + float2(0.0, 0.0);
    float2 c1 = i + float2(1.0, 0.0);
    float2 c2 = i + float2(0.0, 1.0);
    float2 c3 = i + float2(1.0, 1.0);
    float r0 = unity_noise_randomValue(c0);
    float r1 = unity_noise_randomValue(c1);
    float r2 = unity_noise_randomValue(c2);
    float r3 = unity_noise_randomValue(c3);

    float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
    float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
    float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
    return t;
}

float Unity_SimpleNoise_float(float2 UV)
{
    float t = 0.0;

    float freq = pow(2.0, float(0));
    float amp = pow(0.5, float(3-0));
    t += unity_valueNoise(float2(UV.x/freq, UV.y/freq))*amp;

    freq = pow(2.0, float(1));
    amp = pow(0.5, float(3-1));
    t += unity_valueNoise(float2(UV.x/freq, UV.y/freq))*amp;

    freq = pow(2.0, float(2));
    amp = pow(0.5, float(3-2));
    t += unity_valueNoise(float2(UV.x/freq, UV.y/freq))*amp;

    return t;
}