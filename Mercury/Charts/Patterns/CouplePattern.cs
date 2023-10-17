using static Mercury.Charts.Patterns.PatternUtil;

namespace Mercury.Charts.Patterns
{
    /// <summary>
    /// Pattern for 2 quotes
    /// </summary>
    public class CouplePattern : IPattern
    {
        /// <summary>
        /// * Pattern Number
        /// 
        /// - Numbering Convention
        /// 1~9 + 01~13 + 001~196
        /// 
        /// S1. OC TYPE
        ///     1. UU
        ///     2. UD
        ///     3. UE
        ///     4. DU
        ///     5. DD
        ///     6. DE
        ///     7. EU
        ///     8. ED
        ///     9. EE
        ///     
        /// S2. OC PATTERN TYPE
        ///     (1) UU, UD, DU, DD
        ///         01. 100-150, 100-150 (UU) | 100-150, 150-100 (UD) | 150-100, 100-150 (DU) | 150-100, 150-100 (DD)
        ///         02. 100-150, 100-110 | 100-150, 110-100 | 150-100, 100-110 | 150-100, 110-100
        ///         03. 100-150, 100-170 | 100-150, 170-100 | 150-100, 100-170 | 150-100, 170-100
        ///         04. 100-150, 80-150 | 100-150, 150-80 | 150-100, 80-150 | 150-100, 150-80
        ///         05. 100-150, 140-150
        ///         06. 100-150, 80-100
        ///         07. 100-150, 80-120
        ///         08. 100-150, 80-170
        ///         09. 100-150, 110-120
        ///         10. 100-150, 130-170
        ///         11. 100-150, 150-170
        ///         12. 100-150, 50-80
        ///         13. 100-150, 170-200
        ///         
        ///     (2) UE, DE
        ///         01. 100-150, 100-100
        ///         02. 100-150, 150-150
        ///         03. 100-150, 120-120
        ///         04. 100-150, 80-80
        ///         05. 100-150, 200-200
        ///         
        ///     (3) EU, ED
        ///         01. 100-100, 90-100
        ///         02. 100-100, 100-120
        ///         03. 100-100, 90-120
        ///         04. 100-100, 70-80
        ///         05. 100-100, 120-150
        ///         
        ///     (4) EE
        ///         01. 100-100, 100,100
        ///         02. 100-100, 80-80
        ///         03. 100-100, 120-120
        ///         
        /// S3. HL PATTERN TYPE
        ///     (1) CASE: 1268 * 4 = 5072
        ///         01. L6*H6 = 36
        ///             (O-L or C-L) *6
        ///             00. 100-100, 100-100
        ///             01. 100-100, 100-90
        ///             02. 100-90, 100-100
        ///             03. 100-90, 100-90
        ///             04. 100-90, 100-95
        ///             05. 100-90, 100-80
        ///             (O-H or C-H) +
        ///             01. 150-150, 150-150
        ///             02. 150-150, 150-200
        ///             03. 150-200, 150,150
        ///             04. 150-200, 150-200
        ///             05. 150-200, 150-180
        ///             06. 150-200, 150-250
        ///         02. L6*H10 = 60
        ///             (O-L or C-L) *10
        ///             00. 100-100, 100-100
        ///             01. 100-100, 100-90
        ///             02. 100-90, 100-100
        ///             03. 100-90, 100-90
        ///             04. 100-90, 100-95
        ///             05. 100-90, 100-80
        ///             (O-H or C-H) +
        ///             01. 150-150, 110-110
        ///             02. 150-150, 110-120
        ///             03. 150-150, 110-150
        ///             04. 150-150, 110-160
        ///             05. 150-180, 110-110
        ///             06. 150-180, 110-120
        ///             07. 150-180, 110-150
        ///             08. 150-180, 110-160
        ///             09. 150-180, 110-180
        ///             10. 150-180, 110-200
        ///         03. L6*H10 = 60
        ///         04. L10*H6 = 60
        ///         05. L10*H6 = 60
        ///         06. L10*H10 = 100
        ///         07. L10*H10 = 100
        ///         08. L10*H10 = 100
        ///         09. L10*H10 = 100
        ///         10. L10*H10 = 100
        ///         11. L10*H10 = 100
        ///         12. L14*H14 = 196
        ///         13. L14*H14 = 196
        ///         
        ///     (2) CASE: 500 * 2 = 1000
        ///         01. L6*H10 = 60
        ///         02. L10*H6 = 60
        ///         03. L10*H10 = 100
        ///         04. L10*H14 = 140
        ///         05. L14*H10 = 140
        ///         
        ///     (3) CASE : 500 * 2 = 1000
        ///         01. L10*H6 = 60
        ///         02. L6*H10 = 60
        ///         03. L10*H10 = 100
        ///         04. L14*H10 = 140
        ///         05. L10*H14 = 140
        ///         
        ///     (4) CASE : 236
        ///         01. L6*H6 = 36
        ///         02. L10*H10 = 100
        ///         03. L10*H10 = 100
        ///         
        /// CASE SUM: 5072 + 1000 + 1000 + 236 = 7308
        /// </summary>
        public int PatternNumber = 0;

        public CouplePattern(Quote q0, Quote q1, decimal equalThreshold = 0.0005m)
        {
            PatternNumber = 0;
            EqualThreshold = equalThreshold;

            // S1
            switch (Relation(q0.Open, q0.Close))
            {
                case QuoteRelation.Less:
                    switch (Relation(q1.Open, q1.Close))
                    {
                        case QuoteRelation.Less: // UU
                            PatternNumber += 100_000;

                            if (Relation(q0.Open, q1.Open) == QuoteRelation.Equal && Relation(q0.Close, q1.Close) == QuoteRelation.Equal)
                            {
                                PatternNumber += 1000;

                                if (Relation(q0.Open, q0.Low) == QuoteRelation.Equal && Relation(q1.Open, q1.Low) == QuoteRelation.Equal)
                                {
                                    PatternNumber += 0 * 6;
                                }
                                else if (Relation(q0.Open, q0.Low) == QuoteRelation.Equal && Relation(q1.Open, q1.Low) == QuoteRelation.Greater)
                                {
                                    PatternNumber += 1 * 6;
                                }
                                else if (Relation(q0.Open, q0.Low) == QuoteRelation.Greater && Relation(q1.Open, q1.Low) == QuoteRelation.Equal)
                                {
                                    PatternNumber += 2 * 6;
                                }
                                else if (Relation(q0.Open, q0.Low) == QuoteRelation.Greater && Relation(q1.Open, q1.Low) == QuoteRelation.Greater)
                                {
                                    PatternNumber += Relation(q0.Low, q1.Low) switch
                                    {
                                        QuoteRelation.Equal => 3 * 6,
                                        QuoteRelation.Less => 4 * 6,
                                        QuoteRelation.Greater => 5 * 6,
                                        _ => 0
                                    };
                                }

                                if (Relation(q0.Close, q0.High) == QuoteRelation.Equal && Relation(q1.Close, q1.High) == QuoteRelation.Equal)
                                {
                                    PatternNumber += 1;
                                }
                                else if (Relation(q0.Close, q0.High) == QuoteRelation.Equal && Relation(q1.Close, q1.High) == QuoteRelation.Less)
                                {
                                    PatternNumber += 2;
                                }
                                else if (Relation(q0.Close, q0.High) == QuoteRelation.Less && Relation(q1.Close, q1.High) == QuoteRelation.Equal)
                                {
                                    PatternNumber += 3;
                                }
                                else if (Relation(q0.Close, q0.High) == QuoteRelation.Less && Relation(q1.Close, q1.High) == QuoteRelation.Less)
                                {
                                    PatternNumber += Relation(q0.High, q1.High) switch
                                    {
                                        QuoteRelation.Equal => 4,
                                        QuoteRelation.Greater => 5,
                                        QuoteRelation.Less => 6,
                                        _ => 0
                                    };
                                }
                            }
                            else if (Relation(q0.Open, q1.Open) == QuoteRelation.Equal && Relation(q0.Close, q1.Close) == QuoteRelation.Greater)
                            {
                                PatternNumber += 2000;

                                if (Relation(q0.Open, q0.Low) == QuoteRelation.Equal && Relation(q1.Open, q1.Low) == QuoteRelation.Equal)
                                {
                                    PatternNumber += 0 * 10;
                                }
                                else if (Relation(q0.Open, q0.Low) == QuoteRelation.Equal && Relation(q1.Open, q1.Low) == QuoteRelation.Greater)
                                {
                                    PatternNumber += 1 * 10;
                                }
                                else if (Relation(q0.Open, q0.Low) == QuoteRelation.Greater && Relation(q1.Open, q1.Low) == QuoteRelation.Equal)
                                {
                                    PatternNumber += 2 * 10;
                                }
                                else if (Relation(q0.Open, q0.Low) == QuoteRelation.Greater && Relation(q1.Open, q1.Low) == QuoteRelation.Greater)
                                {
                                    PatternNumber += Relation(q0.Low, q1.Low) switch
                                    {
                                        QuoteRelation.Equal => 3 * 10,
                                        QuoteRelation.Less => 4 * 10,
                                        QuoteRelation.Greater => 5 * 10,
                                        _ => 0
                                    };
                                }

                                if (Relation(q0.Close, q0.High) == QuoteRelation.Equal && Relation(q1.Close, q1.High) == QuoteRelation.Equal)
                                {
                                    PatternNumber += 1;
                                }
                                else if (Relation(q0.Close, q0.High) == QuoteRelation.Equal && Relation(q1.Close, q1.High) == QuoteRelation.Less)
                                {
                                    PatternNumber += Relation(q0.Close, q1.High) switch
                                    {
                                        QuoteRelation.Greater => 2,
                                        QuoteRelation.Equal => 3,
                                        QuoteRelation.Less => 4,
                                        _ => 0
                                    };
                                }
                                else if (Relation(q0.Close, q0.High) == QuoteRelation.Less && Relation(q1.Close, q1.High) == QuoteRelation.Equal)
                                {
                                    PatternNumber += 5;
                                }
                                else if (Relation(q0.Close, q0.High) == QuoteRelation.Less && Relation(q1.Close, q1.High) == QuoteRelation.Less)
                                {
                                    PatternNumber += Relation(q0.Close, q1.High) switch
                                    {
                                        QuoteRelation.Greater => 6,
                                        QuoteRelation.Equal => 7,
                                        QuoteRelation.Less => Relation(q0.High, q1.High) switch
                                        {
                                            QuoteRelation.Greater => 8,
                                            QuoteRelation.Equal => 9,
                                            QuoteRelation.Less => 10,
                                            _ => 0
                                        },
                                        _ => 0
                                    };
                                }
                            }
                            else if (Relation(q0.Open, q1.Open) == QuoteRelation.Equal && Relation(q0.Close, q1.Close) == QuoteRelation.Less)
                            {
                                PatternNumber += 3000;

                                if (Relation(q0.Open, q0.Low) == QuoteRelation.Equal && Relation(q1.Open, q1.Low) == QuoteRelation.Equal)
                                {
                                    PatternNumber += 0 * 10;
                                }
                                else if (Relation(q0.Open, q0.Low) == QuoteRelation.Equal && Relation(q1.Open, q1.Low) == QuoteRelation.Greater)
                                {
                                    PatternNumber += 1 * 10;
                                }
                                else if (Relation(q0.Open, q0.Low) == QuoteRelation.Greater && Relation(q1.Open, q1.Low) == QuoteRelation.Equal)
                                {
                                    PatternNumber += 2 * 10;
                                }
                                else if (Relation(q0.Open, q0.Low) == QuoteRelation.Greater && Relation(q1.Open, q1.Low) == QuoteRelation.Greater)
                                {
                                    PatternNumber += Relation(q0.Low, q1.Low) switch
                                    {
                                        QuoteRelation.Equal => 3 * 10,
                                        QuoteRelation.Less => 4 * 10,
                                        QuoteRelation.Greater => 5 * 10,
                                        _ => 0
                                    };
                                }

                                if (Relation(q0.Close, q0.High) == QuoteRelation.Equal && Relation(q1.Close, q1.High) == QuoteRelation.Equal)
                                {
                                    PatternNumber += 1;
                                }
                                else if (Relation(q0.Close, q0.High) == QuoteRelation.Less && Relation(q1.Close, q1.High) == QuoteRelation.Equal)
                                {
                                    PatternNumber += Relation(q0.High, q1.Close) switch
                                    {
                                        QuoteRelation.Less => 2,
                                        QuoteRelation.Equal => 3,
                                        QuoteRelation.Greater => 4,
                                        _ => 0
                                    };
                                }
                                else if (Relation(q0.Close, q0.High) == QuoteRelation.Equal && Relation(q1.Close, q1.High) == QuoteRelation.Less)
                                {
                                    PatternNumber += 5;
                                }
                                else if (Relation(q0.Close, q0.High) == QuoteRelation.Less && Relation(q1.Close, q1.High) == QuoteRelation.Less)
                                {
                                    PatternNumber += Relation(q0.High, q1.Close) switch
                                    {
                                        QuoteRelation.Less => 6,
                                        QuoteRelation.Equal => 7,
                                        QuoteRelation.Greater => Relation(q0.High, q1.High) switch
                                        {
                                            QuoteRelation.Less => 8,
                                            QuoteRelation.Equal => 9,
                                            QuoteRelation.Greater => 10,
                                            _ => 0
                                        },
                                        _ => 0
                                    };
                                }
                            }
                            else if (Relation(q0.Open, q1.Open) == QuoteRelation.Greater && Relation(q0.Close, q1.Close) == QuoteRelation.Equal)
                            {
                                PatternNumber += 4000;
                            }
                            else if (Relation(q0.Open, q1.Open) == QuoteRelation.Less && Relation(q0.Close, q1.Close) == QuoteRelation.Equal)
                            {
                                PatternNumber += 5000;
                            }
                            else if (Relation(q0.Open, q1.Open) == QuoteRelation.Greater && Relation(q0.Close, q1.Close) == QuoteRelation.Greater)
                            {
                                PatternNumber += Relation(q0.Open, q1.Close) switch
                                {
                                    QuoteRelation.Equal => 6000,
                                    QuoteRelation.Less => 7000,
                                    QuoteRelation.Greater => 12000,
                                    _ => 0
                                };
                            }
                            else if (Relation(q0.Open, q1.Open) == QuoteRelation.Greater && Relation(q0.Close, q1.Close) == QuoteRelation.Less)
                            {
                                PatternNumber += 8000;
                            }
                            else if (Relation(q0.Open, q1.Open) == QuoteRelation.Less && Relation(q0.Close, q1.Close) == QuoteRelation.Greater)
                            {
                                PatternNumber += 9000;
                            }
                            else if (Relation(q0.Open, q1.Open) == QuoteRelation.Less && Relation(q0.Close, q1.Close) == QuoteRelation.Less)
                            {
                                PatternNumber += Relation(q0.Close, q1.Open) switch
                                {
                                    QuoteRelation.Greater => 10000,
                                    QuoteRelation.Equal => 11000,
                                    QuoteRelation.Less => 13000,
                                    _ => 0
                                };
                            }
                            break;
                        case QuoteRelation.Greater: // UD
                            PatternNumber += 200_000;
                            break;
                        case QuoteRelation.Equal: // UE
                            PatternNumber += 300_000;
                            break;
                    }
                    break;

                case QuoteRelation.Greater:
                    switch (Relation(q1.Open, q1.Close))
                    {
                        case QuoteRelation.Less: // DU
                            PatternNumber += 400_000;
                            break;
                        case QuoteRelation.Greater: // DD
                            PatternNumber += 500_000;
                            break;
                        case QuoteRelation.Equal: // DE
                            PatternNumber += 600_000;
                            break;
                    }
                    break;

                case QuoteRelation.Equal:
                    switch (Relation(q1.Open, q1.Close))
                    {
                        case QuoteRelation.Less: // EU
                            PatternNumber += 700_000;
                            break;
                        case QuoteRelation.Greater: // ED
                            PatternNumber += 800_000;
                            break;
                        case QuoteRelation.Equal: // EE
                            PatternNumber += 900_000;
                            break;
                    }
                    break;
            }
        }
    }
}
