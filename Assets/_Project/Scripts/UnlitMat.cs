using UnityEngine;
using UnityEngine.Rendering;

namespace TekCan
{
    /// <summary>
    /// VFX'in paylaştığı iki materyal. `Arc`, `Mote` ve dash izi aynı shader kurulumunu
    /// istiyordu; üç kopya olacaktı, tek kaynağa indirildi.
    ///
    /// İkisinin farkı harmanlama:
    ///   • <see cref="Alpha"/>    — normal saydamlık. Zemindeki yaylar: koyu zeminde
    ///                              okunur kalsın, ama ışık gibi parlamasın.
    ///   • <see cref="Additive"/> — eklemeli. Zaman zerreleri ve dash izi: üst üste
    ///                              binince parlasın, "enerji" gibi dursun.
    ///
    /// ⚠️ `Reset()` Bootstrap.ResetStatics'ten çağrılıyor. Play oturumları arası domain
    /// reload kapalıyken Material referansı ölü kalıyor ve sessizce pembe render ediyor.
    /// </summary>
    public static class UnlitMat
    {
        static Material _alpha, _additive;

        public static Material Alpha
        {
            get
            {
                // ⚠️ `??=` KULLANILMAZ: Unity'de yok edilmiş nesne "sahte null"dur,
                // `??` operatörü onu null saymaz ve ölü materyal geri döner.
                if (_alpha == null) _alpha = Build(BlendMode.OneMinusSrcAlpha);
                return _alpha;
            }
        }

        public static Material Additive
        {
            get
            {
                if (_additive == null) _additive = Build(BlendMode.One);
                return _additive;
            }
        }

        static Material Build(BlendMode dstBlend)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var m = new Material(sh);

            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);   // 1 = transparent
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)dstBlend);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }

        public static void Reset()
        {
            _alpha = null;
            _additive = null;
        }
    }
}
