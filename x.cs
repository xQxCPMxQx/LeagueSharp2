        var pLucianX = vLucian.Position.X;
                var pLucianY = vLucian.Position.Y;
                
                var vTarget = SimpleTs.GetTarget(Q1.Range, SimpleTs.DamageType.Physical);
                var pTargetX = vTarget.Position.X;
                var pTargetY = vTarget.Position.Y;

                float vXDistance = 0;
                float vYDistance = 0;

                if (pLucianX > pTargetX)
                    vXDistance = (pLucianX - pTargetX) / 2 - 30; // Lucian in LEFT(X) Side, Target in RIGHT(X) Side
                else
                    vXDistance = (pTargetX - pLucianX) / 2 - 30; // Lucian in RIGHT(X) Side, Target in LEFT(X) Side

                if (pLucianY > pTargetY)
                    vYDistance = (pLucianY - pTargetY) / 2 - 30; // Lucian in UP(Y) Side, Target in DOWN(Y) Side
                else
                    vYDistance = (pTargetY - pLucianY) / 2 - 30; // Lucian in DOWN(Y) Side, Target in DOWN(Y) Side

                var vMinions = MinionManager.GetMinions(vLucian.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.None);
                foreach (var vMinion in vMinions.Where(vMinion => vMinion.IsValidTarget(Q.Range)))
                {
                    

                    if (pTargetX < pLucianX ) // Target my Left (X position)
                    {
                        if (pLucianY > pTargetY) // Lucian Right-Top| Target Left-Bottom
                        {
                            

                        } else //Lucian Right-Bottom | Target left-top
                        {

                        }
                    } else // Target on my Right (X position)
                    {
                        if (pLucianY > pTargetY) // Lucian Left-Top| Target Right-Bottom
                        {

                        }else //Lucian Left-Bottom | Target Right-Top
                        {

                        }
                    }
                }
