float2 ComplexMult(float2 a, float2 b) {
    return float2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
}

float2 EulerFormula(float x) {
    return float2(cos(x), sin(x));
}