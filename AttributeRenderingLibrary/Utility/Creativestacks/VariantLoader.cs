using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace AttributeRenderingLibrary.Utility.Creativestacks
{
    public class VariantLoader
    {
        private ICoreServerAPI serverApi;
        private ILogger logger;
        private List<CollectibleObject> collectiblesToGenerate = new();
        private ConcurrentQueue<CollectibleAndStackGenerationBehavior> collectibleGenerationQueue = new();
        private ConcurrentQueue<CollectibleAndStacks> finishedStacks = new();
        private Dictionary<AssetLocation, VariantEntry[]> worldProperties = new();

        private long tickListenerId = -1;

        private const int MAX_STACKS_PER_TICK = 1000;
        private const string ATTRIBUTE_TYPE_KEY = "types";
#if DEBUG
        private const string DEBUG_FROM_COMBINE = "fromcombine";
        private const string DEBUG_COMBINE_INDEX = "index";
#endif

        private int activeThreadCounter = 0;

        public bool HaveWorkerThreadsFinished = true;

        public event Action WorkerThreadsFinished;

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // TODO: add signal that stops all worker threads and make sure everything's actually disposed
            collectiblesToGenerate.Clear();
            collectiblesToGenerate = null;
            collectibleGenerationQueue.Clear();
            collectibleGenerationQueue = null;
            worldProperties.Clear();
            worldProperties = null;
        }

        public VariantLoader(ICoreServerAPI api)
        {
            serverApi = api;
            logger = api.ModLoader.GetModSystem<Core>().Mod.Logger;
        }

        /// <summary>
        /// Loads all collectibles with the creative itemstack generation behavior,
        /// and populates the creative inventory with the defined attribute-based variants<br/>
        /// Works asynchronously, this method will not run again while worker threads are still active
        /// </summary>
        public void ComposeVariantsAsync()
        {
            if (!HaveWorkerThreadsFinished || activeThreadCounter > 0)
            {
                logger.Warning("Error composing attribute variants; Can only run one generation at a time; Some worker threads are still active");
                return;
            }
            HaveWorkerThreadsFinished = false;

            // lifted from base game, probably fine as-is
            int maxThreads = (serverApi.Server.IsDedicated ? 3 : 8);
            int threads = GameMath.Clamp(Environment.ProcessorCount / 2 - 2, 1, maxThreads);
            if (serverApi.Server.ReducedServerThreads)
            {
                threads = 1;
            }

            ConcurrentQueue<CollectibleObject> processingQueue = new();
            for (int i = 0; i < collectiblesToGenerate.Count; i++)
            {
                processingQueue.Enqueue(collectiblesToGenerate[i]);
            }

            activeThreadCounter = threads;
            for (int i = 0; i < threads; i++)
            {
                TyronThreadPool.QueueTask(StackGenerationWorker, "attributevariantworker" + i.ToString());
            }

            if (tickListenerId == -1)
            {
                var tickTime = (int)(serverApi.Server.Config.TickTime * 1000);
                tickListenerId = serverApi.World.RegisterGameTickListener(AddFinishedStacksToInventory, tickTime, tickTime);
            }
        }

        /// <summary>
        /// Loads all collectibles with the creative itemstack generation behavior,
        /// and populates the creative inventory with the defined attribute-based variants<br/>
        /// Works synchronously, but will not run while worker threads are still active
        /// </summary>
        public void ComposeVariants()
        {
            if (!HaveWorkerThreadsFinished || activeThreadCounter > 0)
            {
                logger.Warning("Error composing attribute variants; Can only run one generation at a time; Some worker threads are still active");
                return;
            }
            HaveWorkerThreadsFinished = false;

            ConcurrentQueue<CollectibleObject> processingQueue = new();
            for (int i = 0; i < collectiblesToGenerate.Count; i++)
            {
                processingQueue.Enqueue(collectiblesToGenerate[i]);
            }

            activeThreadCounter = 1;
            StackGenerationWorker();
            AddFinishedStacksToInventorySynchronous();
        }

        /// <summary>
        /// Adds finished stacks to the creative inventory. Processes <c>MAX_STACKS_PER_TICK</c> items before stopping,
        /// but will always finish a collectible that has been started<br/>
        /// This is to prevent the main thread from locking up when many collectibles should be generated
        /// </summary>
        public void AddFinishedStacksToInventory(float _)
        {
            int ticks = 0;
            CollectibleAndStacks entry;
            List<CreativeTabAndStackList> creativeTabs = new();
            List<JsonItemStack> tabStacks = new();

            while (ticks < MAX_STACKS_PER_TICK && finishedStacks.TryDequeue(out entry))
            {
                var behavior = entry.Collectible.GetCollectibleBehavior<CollectibleBehaviorGenerateCreativeStacks>(true);
                creativeTabs.Clear();
                var colCreativeTabs = behavior.CreativeInventory;

                if (entry.Collectible.CreativeInventoryStacks != null)
                {
                    creativeTabs.AddRange(entry.Collectible.CreativeInventoryStacks);
                }

                foreach (var tab in colCreativeTabs)
                {
                    tabStacks.Clear();
                    foreach (var stack in entry.Stacks)
                    {
                        if (VariantMatchesTab(stack, entry.CompleteVariantGroups, tab.Value))
                        {
                            tabStacks.Add(stack);
                        }
                    }

                    creativeTabs.Add(new CreativeTabAndStackList
                    {
                        Tabs = [tab.Key],
                        Stacks = tabStacks.ToArray()
                    });
                }

                entry.Collectible.CreativeInventoryStacks = creativeTabs.ToArray();
                ticks += entry.Stacks.Length;
            }

            if (HaveWorkerThreadsFinished && finishedStacks.IsEmpty)
            {
                serverApi.World.UnregisterGameTickListener(tickListenerId);
                tickListenerId = -1;
                // for now we'll assume we are done with the loader if no more collectibles to load
                Dispose();
            }
        }

        /// <summary>
        /// Synchronously adds finished stacks to the creative inventory
        /// </summary>
        public void AddFinishedStacksToInventorySynchronous()
        {
            CollectibleAndStacks entry;
            List<CreativeTabAndStackList> creativeTabs = new();
            List<JsonItemStack> tabStacks = new();

            while (finishedStacks.TryDequeue(out entry))
            {
                var behavior = entry.Collectible.GetCollectibleBehavior<CollectibleBehaviorGenerateCreativeStacks>(true);
                creativeTabs.Clear();
                var colCreativeTabs = behavior.CreativeInventory;

                if (entry.Collectible.CreativeInventoryStacks != null)
                {
                    creativeTabs.AddRange(entry.Collectible.CreativeInventoryStacks);
                }

                foreach (var tab in colCreativeTabs)
                {
                    tabStacks.Clear();
                    foreach (var stack in entry.Stacks)
                    {
                        if (VariantMatchesTab(stack, entry.CompleteVariantGroups, tab.Value))
                        {
                            tabStacks.Add(stack);
                        }
                    }

                    creativeTabs.Add(new CreativeTabAndStackList
                    {
                        Tabs = [tab.Key],
                        Stacks = tabStacks.ToArray()
                    });
                }

                entry.Collectible.CreativeInventoryStacks = creativeTabs.ToArray();
            }

            // for now we'll assume we are done with the loader if no more collectibles to load
            Dispose();
        }

        /// <summary>
        /// Checks if an itemstack matches any of the conditions associated with a certain tab
        /// </summary>
        /// <param name="stack">The stack to check</param>
        /// <param name="groups">All valid variant groups for this stack</param>
        /// <param name="matchers">The tab matchers to check against</param>
        /// <returns></returns>
        private bool VariantMatchesTab(JsonItemStack stack, Dictionary<string, CombineState> groups, string[] matchers)
        {
            var attribute = stack.Attributes[ATTRIBUTE_TYPE_KEY];
            int matchingVariants;
            string subMatch;

            foreach (string matcher in matchers)
            {
                matchingVariants = 0;
                if (matcher == "*") return true;

                var subMatchers = GetMatchersByCode(matcher);

                foreach (var variant in groups.Values)
                {
                    if (!groups.ContainsKey(variant.Code)) continue;
                    if (subMatchers.TryGetValue(variant.Code, out subMatch))
                    {
                        if (WildcardUtil.Match(subMatch, attribute[variant.Code].AsString()))
                        {
                            matchingVariants++;
                        }
                    }
                }

                // If all variants in a group match, the stack should be added to the tab
                if (matchingVariants == groups.Count)
                {
                    return true; 
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the individual search strings by variant group code
        /// </code>
        /// </summary>
        /// <param name="matcher"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetMatchersByCode(string matcher)
        {
            string[] matchGroups = matcher.Split("::");
            string[] groupEntry;

            Dictionary<string, string> matchersByCode = new();

            foreach (var group in matchGroups)
            {
                groupEntry = group.Split('-', 2);
                if (groupEntry.Length != 2) continue;

                matchersByCode[groupEntry[0]] = groupEntry[1];
            }

            return matchersByCode;
        }

        /// <summary>
        /// Collects all the collectibles that have the <c>CollectibleBehaviorGenerateCreativeStacks</c> behavior defined
        /// and preloads them into a concurrent queue for later processing
        /// </summary>
        public void CollectCollectibleObjectsToGenerate()
        {
            collectibleGenerationQueue.Clear();

            CollectibleBehaviorGenerateCreativeStacks behavior;

            foreach (var collectible in serverApi.World.Collectibles)
            {
                behavior = collectible.GetBehavior<CollectibleBehaviorGenerateCreativeStacks>();
                if (behavior != null && behavior.CreativeInventory.Count > 0)
                {
                    collectibleGenerationQueue.Enqueue(new CollectibleAndStackGenerationBehavior
                    {
                        CollectibleObject = collectible,
                        CollectibleBehavior = behavior
                    });

                    logger.Debug($"Found collectible {collectible.Code} with generate creative stack behavior");
                }
            }

            logger.Debug($"Found total of {collectibleGenerationQueue.Count} different collectibles to process");
        }

        /// <summary>
        /// Populates all the worldproperty variants that should be preloaded
        /// </summary>
        public void CollectVariantsFromWorldProperties()
        {
            worldProperties.Clear();

            Dictionary<int, AssetLocation> locations = new();

            CollectibleBehaviorGenerateCreativeStacks behavior;
            foreach (var entry in collectibleGenerationQueue)
            {
                behavior = entry.CollectibleObject.GetCollectibleBehavior<CollectibleBehaviorGenerateCreativeStacks>(true);
                if (behavior == null) continue;

                LoadWorldPropertiesForBehavior(behavior, locations);
            }

            StandardWorldProperty worldProperty;
            List<VariantEntry> variants = new();
            AssetLocation modLocation;

            foreach (var location in locations.Values)
            {
                modLocation = location.Clone();
                modLocation.WithPathPrefixOnce("worldproperties/").WithPathAppendixOnce(".json");
                worldProperty = serverApi.Assets.Get<StandardWorldProperty>(modLocation);

                if (worldProperty == null)
                {
                    logger.Warning("Worldproperty with location {0} was not found", location);
                    continue;
                }

                if (worldProperty.Code == null || worldProperty.Variants == null)
                {
                    logger.Warning("Error in worldproperties {0}, code or variants is null, won't load this property", location);
                    continue;
                }

                variants.Clear();
                for (int i = 0; i < worldProperty.Variants.Length; i++)
                {
                    if (worldProperty.Variants[i].Code == null)
                    {
                        logger.Warning("Error in worldproperties {0}, variant {1}, code is null, won't load this variant", location, i);
                        continue;
                    }

                    variants.Add(new VariantEntry
                    {
                        Code = worldProperty.Variants[i].Code.Path
                    });
                }

                worldProperties[location] = variants.ToArray();
            }

            logger.Debug($"Collected a total of {worldProperties.Count} different worldproperties to use across all collectibles");
        }

        /// <summary>
        /// Collects all worldproperty asset locations that need to be loaded and puts them into a neat dictionary by hash code
        /// </summary>
        /// <param name="behavior">The behavior that defines the variants</param>
        /// <param name="propertyLocations">The dictionary holding all worldproperty asset locations that should be loaded across all collectibles</param>
        private void LoadWorldPropertiesForBehavior(CollectibleBehaviorGenerateCreativeStacks behavior, Dictionary<int, AssetLocation> propertyLocations)
        {
            foreach (var group in behavior.AttributeVariantGroups)
            {
                if (group.LoadFromProperties != null)
                {
                    propertyLocations.TryAdd(group.LoadFromProperties.GetHashCode(), group.LoadFromProperties);
                }
                if (group.LoadFromPropertiesCombine != null)
                {
                    foreach (var location in group.LoadFromPropertiesCombine)
                    {
                        propertyLocations.TryAdd(location.GetHashCode(), location);
                    }
                }
            }
        }

        /// <summary>
        /// Pulls collectibles to process from the queue, generates all the variant attributes for that collectible,
        /// and places the finished itemstacks in the output queue to be processed<br/>
        /// Useable for both single- and multithreaded
        /// </summary>
        private void StackGenerationWorker()
        {
            CollectibleAndStackGenerationBehavior operand;
            Dictionary<string, CombineState> variantGroups = new();

            List<JsonObject> variantAttributes;
            JsonItemStack[] resultStacks;

            while (collectibleGenerationQueue.TryDequeue(out operand))
            {
                CollectVariantGroupsForInstance(operand.CollectibleBehavior, variantGroups);
#if DEBUG
                logger.Debug($"found {variantGroups.Count} variant groups for collectible {operand.CollectibleObject.Code}");
                foreach (var entry in variantGroups)
                {
                    logger.Debug($"Variant group {entry.Key} has {entry.Value.States.Length} entries: {string.Join(", ", entry.Value)}");
                }
#endif
                variantAttributes = ComposeVariantAttributes(variantGroups);
                resultStacks = ComposeStacksFromVariants(operand.CollectibleObject, variantAttributes);

                finishedStacks.Enqueue(new CollectibleAndStacks
                {
                    Collectible = operand.CollectibleObject,
                    Stacks = resultStacks,
                    CompleteVariantGroups = variantGroups
                });
            }

            int remainingThreads = Interlocked.Decrement(ref activeThreadCounter);
            if (remainingThreads == 0)
            {
                HaveWorkerThreadsFinished = true;
                WorkerThreadsFinished?.Invoke();
            }
        }
        private JsonItemStack[] ComposeStacksFromVariants(CollectibleObject forCollectible, List<JsonObject> attributes)
        {
            JsonItemStack[] result = new JsonItemStack[attributes.Count];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new JsonItemStack
                {
                    Code = forCollectible.Code,
                    Type = forCollectible.ItemClass,
                    Attributes = attributes[i]
                };
                // resolve seems to only perform read operations on WorldAccessor,
                // so this should be fine to do off the main thread I think
#if DEBUG
                result[i].Resolve(serverApi.World, "attributerenderinglibrary");
#else
                result[i].Resolve(serverApi.World, "attributerenderinglibrary", false);
#endif
            }

            return result;
        }

        /// <summary>
        /// Creates all possible combinations of attributes that are defined in the variants
        /// </summary>
        /// <param name="variants">The variant groups to use for generation</param>
        /// <returns>A list of all valid attribute permutations as <c>JsonObject</c></returns>
        private List<JsonObject> ComposeVariantAttributes(Dictionary<string, CombineState> variants)
        {
            List<JsonObject> attributes = new();

            // stage 1; all non-multiply variants need to be handled individually
            foreach (var entry in variants)
            {
                switch (entry.Value.Combine)
                {
                    case EnumCombination.Add:
                        ComposeAttributesAdd(entry.Value, attributes);
                        break;
                    case EnumCombination.SelectiveMultiply:
                        ComposeAttributesSelectiveMultiply(entry.Value, variants, attributes);
                        break;
                }
            }

            // stage 2; all multiply variants can be generated at once for efficiency
            ComposeAttributesMultiply(variants, attributes);

            return attributes;
        }

        /// <summary>
        /// Creates all the attributes for the given 'add' variant group
        /// </summary>
        /// <param name="group">The add variant group</param>
        /// <param name="attributes">The list of attributes to append to</param>
        private void ComposeAttributesAdd(CombineState group, List<JsonObject> attributes)
        {
            JObject jattribute;
            JObject jtemplate;
            string state;

            for (int i = 0; i < group.States.Length; i++)
            {
                state = group.States[i];

                jattribute = new JObject();
                jtemplate = new JObject();
                jattribute[group.Code] = JToken.FromObject(state);
                jtemplate[ATTRIBUTE_TYPE_KEY] = jattribute;
#if DEBUG
                jtemplate[DEBUG_FROM_COMBINE] = "add";
                jtemplate[DEBUG_COMBINE_INDEX] = i.ToString();
#endif

                attributes.Add(new JsonObject(jtemplate));
            }
        }

        /// <summary>
        /// Creates all the attributes for the 'multiply' (default) variant groups<br/>
        /// Automatically filters out all non-multiply groups from the input groups
        /// </summary>
        /// <param name="variants">The list of all variants</param>
        /// <param name="attributes">The list of attributes to append to</param>
        private void ComposeAttributesMultiply(Dictionary<string, CombineState> variants, List<JsonObject> attributes)
        {
            bool initialize = true;

            JObject template;

            int totalLength = 1;
            int remainingLength;
            int chunkSize;
            int stateIndex;

            foreach (var entry in variants)
            {
                if (entry.Value.Combine != EnumCombination.Multiply) continue;
                totalLength *= entry.Value.States.Length;
            }

            remainingLength = totalLength;
            chunkSize = totalLength;

            JsonObject[] multiplyVariants = new JsonObject[totalLength];

            foreach (var group in variants)
            {
                if (group.Value.Combine != EnumCombination.Multiply) continue;

                remainingLength = remainingLength / group.Value.States.Length;

                for (int i = 0; i < totalLength; i++)
                {
                    if (initialize)
                    {
                        template = new JObject();
                        template[ATTRIBUTE_TYPE_KEY] = new JObject();
                        multiplyVariants[i] = new JsonObject(template);

#if DEBUG
                        multiplyVariants[i].Token[DEBUG_FROM_COMBINE] = "multiply";
                        multiplyVariants[i].Token[DEBUG_COMBINE_INDEX] = i.ToString();
#endif
                    }

                    stateIndex = (i % chunkSize) / remainingLength;
                    multiplyVariants[i].Token[ATTRIBUTE_TYPE_KEY][group.Value.Code] = JToken.FromObject(group.Value.States[stateIndex]);
                }
                
                initialize = false;
                chunkSize = chunkSize / group.Value.States.Length;
            }

            // TODO:
            //  in-place creation in the attributes list, instead of using intermediate multiplyVariants array
            attributes.AddRange(multiplyVariants);
        }

        /// <summary>
        /// Creates all attributes for the given 'selective multiply' variant group
        /// </summary>
        /// <param name="group">The selective multiply group</param>
        /// <param name="variants">All variant groups to search for the 'onVariant' group</param>
        /// <param name="attributes">The attribute list to append to</param>
        private void ComposeAttributesSelectiveMultiply(CombineState group, Dictionary<string, CombineState> variants, List<JsonObject> attributes)
        {
            JsonObject template;
            string state;

            for (int i = 0; i < group.States.Length; i++)
            {
                state = group.States[i];

                template = new JsonObject(new JObject());
                template.Token[group.Code] = JToken.FromObject(state);
#if DEBUG
                template.Token[DEBUG_FROM_COMBINE] = "multiplyselective";
                template.Token[DEBUG_COMBINE_INDEX] = i.ToString();
#endif

                foreach (var entry in variants)
                {
                    if (entry.Value.Code == group.Code) continue;
                    if (entry.Value.Code != group.OnVariant) continue;

                    foreach (var subState in entry.Value.States)
                    {
                        template.Token[entry.Key] = subState;
                        attributes.Add(template.Clone());
                    }

                    // only 1 onVariant possible, skip
                    break;
                }
            }
        }

        /// <summary>
        /// Collects and combines the specific different variant groups that apply to a specific combination of collectible and 
        /// </summary>
        /// <param name="operand"></param>
        /// <param name="variantGroups"></param>
        private void CollectVariantGroupsForInstance(CollectibleBehaviorGenerateCreativeStacks stackBehavior, Dictionary<string, CombineState> variantGroups)
        {
            VariantEntry[] worldVariants;
            HashSet<string> uniqueVariantCodes = new();
            variantGroups.Clear();

            foreach (var variantGroup in stackBehavior.AttributeVariantGroups)
            {
                /*if (!IsVariantCodeValid(variantGroup))
                {
                    continue;
                }*/
                uniqueVariantCodes.Clear();

                if (variantGroup.States != null && variantGroup.States.Length > 0)
                {

                    uniqueVariantCodes.AddRange(variantGroup.States);
                }

                if (variantGroup.LoadFromProperties != null && worldProperties.TryGetValue(variantGroup.LoadFromProperties, out worldVariants))
                {
                    foreach (var variant in worldVariants)
                    {
                        // uniqueVariantCodes.AddRange(variant.Codes);
                        uniqueVariantCodes.Add(variant.Code);
                    }
                }

                if (variantGroup.LoadFromPropertiesCombine != null)
                {
                    foreach (var variantCombineGroup in variantGroup.LoadFromPropertiesCombine)
                    {
                        if (!worldProperties.TryGetValue(variantCombineGroup, out worldVariants))
                        {
                            continue;
                        }

                        foreach (var variant in worldVariants)
                        {
                            uniqueVariantCodes.Add(variant.Code);
                        }
                    }
                }

                if (uniqueVariantCodes.Count > 0)
                {
                    variantGroups[variantGroup.Code] = new CombineState
                    {
                        Combine = variantGroup.Combine,
                        States = uniqueVariantCodes.ToArray(),
                        Code = variantGroup.Code,
                        OnVariant = variantGroup.OnVariant
                    };
                }
            }
        }
    }
}
