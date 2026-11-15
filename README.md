# Pomelo

Videojuego 2D (Unity 6, URP 2D). Escena principal: `Assets/Scenes/Nivel1_Templo.unity`.

## Cómo trabajamos con git

- **Una rama por persona o por tarea** (por ejemplo `caro/aviso-checkpoints`, `estefi/rayos-de-luz`). Nadie trabaja directo sobre `main`.
- **Antes de empezar y antes de mergear: Pull de `main`** y traerlo a la rama (en GitHub Desktop: *Branch → Update from main*).
- Para llevar la rama a `main`, abrir un **Pull Request** y que otra persona lo revise antes de mergear.
- **Conflictos en la escena** (`Nivel1_Templo.unity`): nunca resolver eligiendo "mi versión" o "la otra" para todo el archivo, porque se borran los cambios del resto del equipo sin aviso (ya pasó en `d98a75e` y en `8e64fa1`). Avisar al equipo y combinarlo a mano.
- Commits chicos, uno por cambio, con un título que diga qué cambió.

## Carpetas

Todo lo propio va en `Assets/` en carpetas que empiezan con `_` y mayúscula: `_Animaciones`, `_Enemigos`, `_Entorno`, `_Fondo`, `_Fuentes`, `_Items`, `_Materiales`, `_Musica`, `_Particulas`, `_Personaje`, `_Prefabs`, `_Scripts`, `_Sonidos`, `_UI`. Los scripts de editor van en `Editor`. No dejar archivos sueltos en la raíz de `Assets`. Para mover o renombrar, hacerlo desde Unity (así no se rompen las referencias).

## Orden de dibujo

Sorting layers, de atrás hacia adelante: Fondo, Edificios, FondoDetalles, Decorado, Escenario, Personajes, Frente, Efectos, Interfaz. Dentro de cada capa se usan órdenes chicos (1 a ~30).

## Mensajes de consola

Los `Debug.Log` de cada script salen solo si se tilda **Mostrar Debug** en su Inspector (apagado por defecto).
