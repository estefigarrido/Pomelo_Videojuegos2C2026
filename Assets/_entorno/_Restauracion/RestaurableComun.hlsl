#ifndef POMELO_RESTAURABLE
#define POMELO_RESTAURABLE
TEXTURE2D(_MascaraSectores); SAMPLER(sampler_MascaraSectores);
        float4 _RectSectores;

        // 0..1: cuanto color recupero este punto del mundo (1 = todo el color)
        float Revelado(float2 mundo, float4 blob, float semilla)
        {
            float2 d = mundo - blob.xy;
            float ang = atan2(d.y, d.x);
            // forma de blob: el radio ondula (como el vector del Figma, que no es un circulo)
            float onda = 1.0 + 0.22 * sin(ang * 3.0 + semilla) + 0.12 * sin(ang * 5.0 - semilla * 1.7) + 0.06 * sin(ang * 9.0 + semilla * 2.3);
            float r = blob.z * onda;
            return 1.0 - smoothstep(r - blob.w, r + blob.w, length(d));
        }

        float2 Sector(float2 mundo)
        {
            if (_RectSectores.z <= 0.0) return float2(0.0, 0.0);   // sin RestauracionTemplo en la escena: todo roto
            float2 uv = (mundo - _RectSectores.xy) * _RectSectores.zw;
            if (any(uv < 0.0) || any(uv > 1.0)) return float2(0.0, 0.0);
            return SAMPLE_TEXTURE2D(_MascaraSectores, sampler_MascaraSectores, uv).rg;
        }

        // aplica el gris/blob y la mascara de sectores al color del sprite
        half4 Restaurar(half4 c, float2 mundo, float gris, float4 blob, float semilla, float modo, half4 colorEstela, float satGris, float aclarar)
        {
            if (gris > 0.001)
            {
                float g = gris * (1.0 - Revelado(mundo, blob, semilla));
                half lum = dot(c.rgb, half3(0.299, 0.587, 0.114));
                half3 palido = lerp(lum.xxx, c.rgb, satGris);
                palido = lerp(palido, 1.0.xxx, aclarar * lum);
                c.rgb = lerp(c.rgb, palido, g);
            }
            if (abs(modo) > 0.5)
            {
                float2 sec = Sector(mundo);
                float m = sec.x;
                float visible = modo > 0.0 ? m : 1.0 - m;
                // estela: brilla en el borde que avanza (solo del lado del mapa arreglado)
                float borde = sec.y;
                if (modo > 0.0) c.rgb += colorEstela.rgb * colorEstela.a * borde * borde * 0.9;
                c.a *= visible;
            }
            return c;
        }
    
#endif
