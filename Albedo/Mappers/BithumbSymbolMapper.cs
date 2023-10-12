using Albedo.Enums;

using System.Collections.Generic;

namespace Albedo.Mappers
{
    public class BithumbSymbolMapper
    {
        /// <summary>
        /// 빗썸은 한글 코인명을 지원하지 않아서 따로 매핑을 한다.
        /// </summary>
        static Dictionary<string, string> values = new()
        {
            // KRW 코인
            { "BTC", "비트코인" },
            { "ETH", "이더리움" },
            { "XRP", "리플" },
            { "SUI", "수이" },
            { "APM", "에이피엠 코인" },
            { "CTC", "크레딧코인" },
            { "KLAY", "클레이튼" },
            { "GOM2", "고머니2" },
            { "SOL", "솔라나" },
            { "ANKR", "앵커" },
            { "MLK", "밀크" },
            { "DOGE", "도지코인" },
            { "TRX", "트론" },
            { "ACH", "알케미페이" },
            { "XPR", "프로톤" },
            { "VELO", "벨로프로토콜" },
            { "BCH", "비트코인 캐시" },
            { "FLZ", "펠라즈" },
            { "ARB", "아비트럼" },
            { "LDO", "리도다오" },
            { "GALA", "갈라" },
            { "ADA", "에이다" },
            { "CENNZ", "센트럴리티" },
            { "ETC", "이더리움 클래식" },
            { "SAND", "샌드박스" },
            { "ONT", "온톨로지" },
            { "EOS", "이오스" },
            { "WOM", "왐토큰" },
            { "BSV", "비트코인에스브이" },
            { "OP", "옵티미즘" },
            { "SXP", "솔라" },
            { "GRT", "더그래프" },
            { "FANC", "팬시" },
            { "LM", "레저메타" },
            { "XYM", "심볼" },
            { "RLY", "랠리" },
            { "MATIC", "폴리곤" },
            { "ARPA", "알파" },
            { "MBX", "마브렉스" },
            { "WOZX", "이포스" },
            { "TAVA", "알타바" },
            { "XEC", "이캐시" },
            { "YFI", "연파이낸스" },
            { "LN", "링크" },
            { "MTL", "메탈" },
            { "CRTS", "크라토스" },
            { "GMT", "스테픈" },
            { "AMO", "아모코인" },
            { "LINK", "체인링크" },
            { "QTCON", "퀴즈톡" },
            { "TRV", "트러스트버스" },
            { "BOA", "보아" },
            { "FLR", "플레어" },
            { "BLUR", "블러" },
            { "APT", "앱토스" },
            { "QTUM", "퀀텀" },
            { "XCN", "오닉스코인" },
            { "INJ", "인젝티브" },
            { "GXA", "갤럭시아" },
            { "CTXC", "코르텍스" },
            { "LINA", "리니어파이낸스" },
            { "DOT", "폴카닷" },
            { "ZIL", "질리카" },
            { "CON", "코넌" },
            { "GRACY", "그레이시" },
            { "REN", "렌" },
            { "ANV", "애니버스" },
            { "XLM", "스텔라루멘" },
            { "EGG", "네스트리" },
            { "BLY", "블로서리" },
            { "FIT", "300피트 네트워크" },
            { "CKB", "너보스" },
            { "WAVES", "웨이브" },
            { "VIX", "빅스코" },
            { "STAT", "스탯" },
            { "AZIT", "아지트" },
            { "CHZ", "칠리즈" },
            { "AXS", "엑시인피니티" },
            { "BNB", "바이낸스코인" },
            { "JST", "저스트" },
            { "ONG", "온톨로지가스" },
            { "ICX", "아이콘" },
            { "OAS", "오아시스" },
            { "VET", "비체인" },
            { "MANA", "디센트럴랜드" },
            { "STRAX", "스트라티스" },
            { "HIVE", "하이브" },
            { "EL", "엘리시아" },
            { "RPL", "로켓풀" },
            { "ATOM", "코스모스" },
            { "BORA", "보라" },
            { "LBL", "레이블" },
            { "GHX", "게이머코인" },
            { "XPLA", "엑스플라" },
            { "RSR", "리저브라이트" },
            { "POLA", "폴라리스 쉐어" },
            { "AERGO", "아르고" },
            { "OBSR", "옵저버" },
            { "T", "쓰레스홀드" },
            { "DAI", "다이" },
            { "JASMY", "재스미코인" },
            { "LPT", "라이브피어" },
            { "UMA", "우마" },
            { "PUNDIX", "펀디엑스" },
            { "ALGO", "알고랜드" },
            { "SOFI", "라이파이낸스" },
            { "SHIB", "시바이누" },
            { "MEV", "미버스" },
            { "MIX", "믹스마블" },
            { "BIOT", "바이오패스포트" },
            { "ATOLO", "라이즌" },
            { "ADP", "어댑터 토큰" },
            { "MXC", "머신익스체인지코인" },
            { "FITFI", "스텝앱" },
            { "BTG", "비트코인 골드" },
            { "NPT", "네오핀" },
            { "OCEAN", "오션프로토콜" },
            { "WOO", "우네트워크" },
            { "WICC", "웨이키체인" },
            { "CTSI", "카르테시" },
            { "AVAX", "아발란체" },
            { "TFUEL", "쎄타퓨엘" },
            { "SWAP", "트러스트스왑" },
            { "EVZ", "이브이지" },
            { "TITAN", "타이탄스왑" },
            { "CELR", "셀러네트워크" },
            { "C98", "코인98" },
            { "CRO", "크로노스" },
            { "XVS", "비너스" },
            { "CAKE", "팬케이크스왑" },
            { "ALT", "아치루트" },
            { "LOOM", "룸네트워크" },
            { "WIKEN", "위드" },
            { "LRC", "루프링" },
            { "MVC", "마일벌스" },
            { "POWR", "파워렛저" },
            { "ENJ", "엔진코인" },
            { "DVI", "디비전" },
            { "SNX", "신세틱스" },
            { "RLC", "아이젝" },
            { "FRONT", "프론티어" },
            { "DYDX", "디와이디엑스" },
            { "TDROP", "티드랍" },
            { "SIX", "식스" },
            { "ALICE", "마이네이버앨리스" },
            { "CSPR", "캐스퍼" },
            { "GLM", "골렘" },
            { "WAXP", "왁스" },
            { "BEL", "벨라프로토콜" },
            { "ORC", "오르빗 체인" },
            { "HOOK", "훅트 프로토콜" },
            { "STEEM", "스팀" },
            { "OXT", "오키드" },
            { "FLOW", "플로우" },
            { "VALOR", "밸러토큰" },
            { "VRA", "베라시티" },
            { "REQ", "리퀘스트" },
            { "XTZ", "테조스" },
            { "MAP", "맵프로토콜" },
            { "ELF", "엘프" },
            { "REI", "레이" },
            { "BOBA", "보바토큰" },
            { "NMR", "뉴메레르" },
            { "META", "메타디움" },
            { "TEMCO", "템코" },
            { "MKR", "메이커" },
            { "FCT2", "피르마체인" },
            { "ASM", "어셈블프로토콜" },
            { "ONIT", "온버프" },
            { "QKC", "쿼크체인" },
            { "STPT", "에스티피" },
            { "ARW", "아로와나토큰" },
            { "MED", "메디블록" },
            { "AAVE", "에이브" },
            { "FX", "펑션엑스" },
            { "BNT", "뱅코르" },
            { "EGLD", "멀티버스엑스" },
            { "KSM", "쿠사마" },
            { "BTT", "비트토렌트" },
            { "BAT", "베이직어텐션토큰" },
            { "UNI", "유니스왑" },
            { "CHR", "크로미아" },
            { "ORBS", "오브스" },
            { "BFC", "바이프로스트" },
            { "SUSHI", "스시스왑" },
            { "SSX", "썸씽" },
            { "DAR", "마인즈 오브 달라니아" },
            { "COS", "콘텐토스" },
            { "DFA", "디파인" },
            { "KNC", "카이버 네트워크" },
            { "UOS", "울트라" },
            { "COMP", "컴파운드" },
            { "NFT", "에이피이앤에프티" },
            { "EFI", "이피니티토큰" },
            { "AQT", "알파쿼크" },
            { "OGN", "오리진프로토콜" },
            { "GO", "고체인" },
            { "SUN", "썬" },
            { "BAL", "밸런서" },
            { "SNT", "스테이터스네트워크토큰" },
            { "PLA", "플레이댑" },
            { "IOST", "이오스트" },
            { "MBL", "무비블록" },
            { "CTK", "셴투" },
            { "1INCH", "1인치" },
            { "THETA", "쎄타토큰" },
            { "COTI", "코티" },
            { "DAO", "다오메이커" },
            { "ZRX", "제로엑스" },
            { "GMX", "지엠엑스" },
            { "SFP", "세이프팔" },
            { "REP", "어거" },
            { "WNCG", "랩트 나인 크로니클 골드" },
            { "FLOKI", "플로키" },
            { "PEPE", "페페" },
            { "STX", "스택스" },
            { "EVER", "에버스케일" },
            { "AGIX", "싱귤래리티넷" },
            { "FET", "페치" },

            // BTC 코인
            { "ROA", "로아코어" },
            { "ENTC", "엔터버튼" },
            { "TALK", "톡큰" },
            { "GPT", "크립토지피티" },
            { "ORB", "오브시티" },
            { "REAP", "립체인" },
            { "DICE", "클레이다이스" },
            { "NEWS", "퍼블리시" },
            { "GRND", "슈퍼워크" },
            { "BERRY", "베리" },
            { "KSP", "클레이스왑" },
            { "AHT", "아하토큰" },
            { "FNSA", "핀시아" }
        };

        public static List<string> Symbols = new()
        {

        };

        public static void Add(string symbol)
        {
            if (Symbols.Contains(symbol))
            {
                return;
            }

            Symbols.Add(symbol);
        }

        public static string GetKoreanName(string symbol)
        {
            var _symbol = symbol[..^4];
            return values.TryGetValue(_symbol, out var name) ? name : _symbol;
        }

        public static PairQuoteAsset GetPairQuoteAsset(string symbol)
        {
            if (symbol.EndsWith("KRW"))
            {
                return PairQuoteAsset.KRW;
            }
            else if (symbol.EndsWith("BTC"))
            {
                return PairQuoteAsset.BTC;
            }
            else
            {
                return PairQuoteAsset.None;
            }
        }
    }
}
