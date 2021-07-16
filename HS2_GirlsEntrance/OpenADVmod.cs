using System;
using System.Collections.Generic;
using Manager;
using Actor;
using ADV;
using AIChara;
using Illusion.Anime;
using UniRx;

namespace HS2_GirlsEntrance
{
    //Open an ADV .unity3d file in lobby
    class OpenADVmod
    {
        private OpenADVmod.PackData packData { get; set; }
        private OpenData openData = new OpenData();
        private BoolReactiveProperty isNowADV = new BoolReactiveProperty(false);
        private bool isADVShow;
        //public Heroine ConciergeHeroine { get; private set; }
        private Heroine ConciergeHeroine { get; set; }
        //public ChaControl ConciergeChaCtrl { get; private set; }
        private ChaControl ConciergeChaCtrl { get; set; }

        public async void OpenADVScene(string _bundle, string _asset, Heroine _heroine, LobbySceneManager sceneLobby, Action _onEnd = null)
        {
            //Indicates that the animation is playing
            HS2_GirlsEntrance.isPlaying = true;

            ConciergeChaCtrl = Singleton<Character>.Instance.GetChara(-1);
            ConciergeHeroine = new Heroine(ConciergeChaCtrl.chaFile, false)
            {
                fixCharaID = -1
            };
            ConciergeHeroine.SetRoot(ConciergeChaCtrl.gameObject);

            await Setup.LoadAsync(sceneLobby.transform);

            isNowADV.Value = true;
            Game instance = Singleton<Game>.Instance;

            packData = new OpenADVmod.PackData();
            packData.SetCommandData(new ICommandData[]
            {
                instance.saveData
            });
            packData.SetParam(new IParams[]
            {
                ConciergeHeroine,
                _heroine
            });
            packData.personal = _heroine.personality;
            packData.isParent = false;

            openData.bundle = _bundle;
            openData.asset = _asset;

            packData.onComplete = delegate ()
            {
                isNowADV.Value = false;
                isADVShow = true;

                Action onEnd = _onEnd;
                if (onEnd != null)
                {
                    onEnd();
                }

                Controller.Table.Get(ConciergeChaCtrl).itemHandler.DisableItems();

                sceneLobby.StartFade(false);
                sceneLobby.SetCharaAnimationAndPosition();

                HS2_GirlsEntrance.isPlaying = false;
            };

            Setup.Open(openData, packData, true, true, true, false);
        }
        
        private class PackData : CharaPackData
        {
            public int personal { get; set; }
            public override List<Program.Transfer> Create()
            {
                List<Program.Transfer> list = base.Create();
                list.Add(Program.Transfer.VAR(new string[]
                {
                    "int",
                    "personal",
                    personal.ToString()
                }));
                return list;
            }
        }
    }
}
