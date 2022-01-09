// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2021 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Trialogue.Ecs;

/// <summary>
///     Entity descriptor.
/// </summary>
public partial struct EcsEntity : IEquatable<EcsEntity>
{
    internal int Id;
    internal ushort Gen;
    internal EcsWorld Owner;
#if DEBUG
    // For using in IDE debugger.
    internal IEcsComponent[] Components
    {
        get
        {
            IEcsComponent[] list = null;
            if (this.IsAlive()) this.GetComponentValues(ref list);
            return list;
        }
    }
#endif

    public static readonly EcsEntity Null = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(in EcsEntity lhs, in EcsEntity rhs)
    {
        return lhs.Id == rhs.Id && lhs.Gen == rhs.Gen;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(in EcsEntity lhs, in EcsEntity rhs)
    {
        return lhs.Id != rhs.Id || lhs.Gen != rhs.Gen;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        unchecked
        {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            var hashCode = (Id * 397) ^ Gen.GetHashCode();
            hashCode = (hashCode * 397) ^ (Owner != null ? Owner.GetHashCode() : 0);
            // ReSharper restore NonReadonlyMemberInGetHashCode
            return hashCode;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object other)
    {
        return other is EcsEntity otherEntity && Equals(otherEntity);
    }

#if DEBUG
    public override string ToString()
    {
        if (this.IsNull()) return "Entity-Null";
        if (!this.IsAlive()) return "Entity-NonAlive";
        Type[] types = null;
        this.GetComponentTypes(ref types);
        var sb = new StringBuilder(512);
        foreach (var type in types)
        {
            if (sb.Length > 0) sb.Append(",");
            sb.Append(type.Name);
        }

        return $"Entity-{Id}:{Gen} [{sb}]";
    }
#endif
    public bool Equals(EcsEntity other)
    {
        return Id == other.Id && Gen == other.Gen && Owner == other.Owner;
    }
}

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
public partial struct EcsEntity
{
    /// <summary>
    ///     Replaces or adds new one component to entity.
    /// </summary>
    /// <typeparam name="T">Type of component.</typeparam>
    /// <param name="entity">Entity.</param>
    /// <param name="item">New value of component.</param>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EcsEntity Update<T>(in T item) where T : struct, IEcsComponent
    {
        ref var entityData = ref this.Owner.GetEntityData(this);
#if DEBUG
        if (entityData.Gen != this.Gen) throw new Exception("Cant add component to destroyed entity.");
#endif
        var typeIdx = EcsComponentType<T>.TypeIndex;
        // check already attached components.
        for (int i = 0, iiMax = entityData.ComponentsCountX2; i < iiMax; i += 2)
            if (entityData.Components[i] == typeIdx)
            {
                ((EcsComponentPool<T>) this.Owner.ComponentPools[typeIdx]).Items[entityData.Components[i + 1]] =
                    item;
                return this;
            }

        // attach new component.
        if (entityData.Components.Length == entityData.ComponentsCountX2)
            Array.Resize(ref entityData.Components, entityData.ComponentsCountX2 << 1);
        entityData.Components[entityData.ComponentsCountX2++] = typeIdx;

        var pool = this.Owner.GetPool<T>();

        var idx = pool.New();
        entityData.Components[entityData.ComponentsCountX2++] = idx;
        pool.Items[idx] = item;
#if DEBUG
        for (var ii = 0; ii < this.Owner.DebugListeners.Count; ii++)
            this.Owner.DebugListeners[ii].OnComponentListChanged(this);
#endif
        this.Owner.UpdateFilters(typeIdx, this, entityData);
        return this;
    }

    /// <summary>
    ///     Returns exist component on entity or adds new one otherwise.
    /// </summary>
    /// <typeparam name="T">Type of component.</typeparam>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get<T>() where T : struct, IEcsComponent
    {
        ref var entityData = ref this.Owner.GetEntityData(this);
#if DEBUG
        if (entityData.Gen != this.Gen) throw new Exception("Cant add component to destroyed entity.");
#endif
        var typeIdx = EcsComponentType<T>.TypeIndex;
        // check already attached components.
        for (int i = 0, iiMax = entityData.ComponentsCountX2; i < iiMax; i += 2)
            if (entityData.Components[i] == typeIdx)
                return ref ((EcsComponentPool<T>) this.Owner.ComponentPools[typeIdx]).Items[
                    entityData.Components[i + 1]];
        // attach new component.
        if (entityData.Components.Length == entityData.ComponentsCountX2)
            Array.Resize(ref entityData.Components, entityData.ComponentsCountX2 << 1);
        entityData.Components[entityData.ComponentsCountX2++] = typeIdx;
    
        var pool = this.Owner.GetPool<T>();

        var idx = pool.New();
        entityData.Components[entityData.ComponentsCountX2++] = idx;
#if DEBUG
        for (var ii = 0; ii < this.Owner.DebugListeners.Count; ii++)
            this.Owner.DebugListeners[ii].OnComponentListChanged(this);
#endif
        this.Owner.UpdateFilters(typeIdx, this, entityData);
        return ref pool.Items[idx];
    }

    /// <summary>
    ///     Checks that component is attached to entity.
    /// </summary>
    /// <typeparam name="T">Type of component.</typeparam>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>() where T : struct
    {
        ref var entityData = ref this.Owner.GetEntityData(this);
#if DEBUG
        if (entityData.Gen != this.Gen) throw new Exception("Cant check component on destroyed entity.");
#endif
        var typeIdx = EcsComponentType<T>.TypeIndex;
        for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2)
            if (entityData.Components[i] == typeIdx)
                return true;
        return false;
    }

    /// <summary>
    ///     Removes component from entity.
    /// </summary>
    /// <typeparam name="T">Type of component.</typeparam>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Del<T>() where T : struct
    {
        var typeIndex = EcsComponentType<T>.TypeIndex;
        ref var entityData = ref this.Owner.GetEntityData(this);
        // save copy to local var for protect from cleanup fields outside.
        var owner = this.Owner;
#if DEBUG
        if (entityData.Gen != this.Gen) throw new Exception("Cant touch destroyed entity.");
#endif
        for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2)
            if (entityData.Components[i] == typeIndex)
            {
                owner.UpdateFilters(-typeIndex, this, entityData);
#if DEBUG
                // var removedComponent = owner.ComponentPools[typeIndex].GetItem (entityData.Components[i + 1]);
#endif
                owner.ComponentPools[typeIndex].Recycle(entityData.Components[i + 1]);
                // remove current item and move last component to this gap.
                entityData.ComponentsCountX2 -= 2;
                if (i < entityData.ComponentsCountX2)
                {
                    entityData.Components[i] = entityData.Components[entityData.ComponentsCountX2];
                    entityData.Components[i + 1] = entityData.Components[entityData.ComponentsCountX2 + 1];
                }
#if DEBUG
                for (var ii = 0; ii < this.Owner.DebugListeners.Count; ii++)
                    this.Owner.DebugListeners[ii].OnComponentListChanged(this);
#endif
                break;
            }

        // unrolled and inlined Destroy() call.
        if (entityData.ComponentsCountX2 == 0)
        {
            owner.RecycleEntityData(this.Id, ref entityData);
#if DEBUG
            for (var ii = 0; ii < this.Owner.DebugListeners.Count; ii++)
                owner.DebugListeners[ii].OnEntityDestroyed(this);
#endif
        }
    }

    /// <summary>
    ///     Creates copy of entity with all components.
    /// </summary>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EcsEntity Copy()
    {
        var owner = this.Owner;
#if DEBUG
        if (owner == null) throw new Exception("Cant copy invalid entity.");
#endif
        ref var srcData = ref owner.GetEntityData(this);
#if DEBUG
        if (srcData.Gen != this.Gen) throw new Exception("Cant copy destroyed entity.");
#endif
        var dstEntity = owner.NewEntity(this.Get<ComponentInfo>().Name);
        ref var dstData = ref owner.GetEntityData(dstEntity);
        if (dstData.Components.Length < srcData.ComponentsCountX2)
            dstData.Components = new int[srcData.Components.Length];
        dstData.ComponentsCountX2 = 0;
        for (int i = 0, iiMax = srcData.ComponentsCountX2; i < iiMax; i += 2)
        {
            var typeIdx = srcData.Components[i];
            var pool = owner.ComponentPools[typeIdx];
            var dstItemIdx = pool.New();
            dstData.Components[i] = typeIdx;
            dstData.Components[i + 1] = dstItemIdx;
            pool.CopyData(srcData.Components[i + 1], dstItemIdx);
            dstData.ComponentsCountX2 += 2;
            owner.UpdateFilters(typeIdx, dstEntity, dstData);
        }
#if DEBUG
        for (var ii = 0; ii < owner.DebugListeners.Count; ii++)
            owner.DebugListeners[ii].OnComponentListChanged(this);
#endif
        return dstEntity;
    }

    /// <summary>
    ///     Adds copies of source entity components
    ///     on target entity (overwrite exists) and
    ///     removes source entity.
    /// </summary>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveTo(in EcsEntity target)
    {
#if DEBUG
        if (!this.IsAlive()) throw new Exception("Cant move from invalid entity.");
        if (!target.IsAlive()) throw new Exception("Cant move to invalid entity.");
        if (this.Owner != target.Owner) throw new Exception("Cant move data between worlds.");
        if (this.AreEquals(target)) throw new Exception("Source and target entities are same.");
        var componentsListChanged = false;
#endif
        var owner = this.Owner;
        ref var srcData = ref owner.GetEntityData(this);
        ref var dstData = ref owner.GetEntityData(target);
        if (dstData.Components.Length < srcData.ComponentsCountX2)
            dstData.Components = new int[srcData.Components.Length];
        for (int i = 0, iiMax = srcData.ComponentsCountX2; i < iiMax; i += 2)
        {
            var typeIdx = srcData.Components[i];
            var pool = owner.ComponentPools[typeIdx];
            var j = dstData.ComponentsCountX2 - 2;
            // search exist component on target.
            for (; j >= 0; j -= 2)
                if (dstData.Components[j] == typeIdx)
                    break;
            if (j >= 0)
            {
                // found, copy data.
                pool.CopyData(srcData.Components[i + 1], dstData.Components[j + 1]);
            }
            else
            {
                // add new one.
                if (dstData.Components.Length == dstData.ComponentsCountX2)
                    Array.Resize(ref dstData.Components, dstData.ComponentsCountX2 << 1);
                dstData.Components[dstData.ComponentsCountX2] = typeIdx;
                var idx = pool.New();
                dstData.Components[dstData.ComponentsCountX2 + 1] = idx;
                dstData.ComponentsCountX2 += 2;
                pool.CopyData(srcData.Components[i + 1], idx);
                owner.UpdateFilters(typeIdx, target, dstData);
#if DEBUG
                componentsListChanged = true;
#endif
            }
        }
#if DEBUG
        if (componentsListChanged)
            for (var ii = 0; ii < owner.DebugListeners.Count; ii++)
                owner.DebugListeners[ii].OnComponentListChanged(target);
#endif
        this.Destroy();
    }

    /// <summary>
    ///     Gets component index at component pool.
    ///     If component doesn't exists "-1" will be returned.
    /// </summary>
    /// <typeparam name="T">Type of component.</typeparam>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetComponentIndexInPool<T>() where T : struct
    {
        ref var entityData = ref this.Owner.GetEntityData(this);
#if DEBUG
        if (entityData.Gen != this.Gen) throw new Exception("Cant check component on destroyed entity.");
#endif
        var typeIdx = EcsComponentType<T>.TypeIndex;
        for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2)
            if (entityData.Components[i] == typeIdx)
                return entityData.Components[i + 1];
        return -1;
    }

    /// <summary>
    ///     Compares entities.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AreEquals(in EcsEntity rhs)
    {
        return this.Id == rhs.Id && this.Gen == rhs.Gen;
    }

    /// <summary>
    ///     Compares internal Ids without Gens check. Use carefully!
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AreIdEquals(in EcsEntity rhs)
    {
        return this.Id == rhs.Id;
    }

    /// <summary>
    ///     Gets internal identifier.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetInternalId()
    {
        return this.Id;
    }

    /// <summary>
    ///     Gets internal generation.
    /// </summary>
    public int GetInternalGen()
    {
        return this.Gen;
    }

    /// <summary>
    ///     Gets internal world.
    /// </summary>
    public EcsWorld GetInternalWorld()
    {
        return this.Owner;
    }

    /// <summary>
    ///     Gets ComponentRef wrapper to keep direct reference to component.
    /// </summary>
    /// <param name="entity">Entity.</param>
    /// <typeparam name="T">Component type.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EcsComponentRef<T> Ref<T>() where T : struct, IEcsComponent
    {
        ref var entityData = ref this.Owner.GetEntityData(this);
#if DEBUG
        if (entityData.Gen != this.Gen) throw new Exception("Cant wrap component on destroyed entity.");
#endif
        var typeIdx = EcsComponentType<T>.TypeIndex;
        for (int i = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2)
            if (entityData.Components[i] == typeIdx)
                return ((EcsComponentPool<T>) this.Owner.ComponentPools[entityData.Components[i]]).Ref(
                    entityData.Components[i + 1]);
#if DEBUG
        throw new Exception($"\"{typeof(T).Name}\" component not exists on entity for wrapping.");
#else
            return default;
#endif
    }

    /// <summary>
    ///     Removes components from entity and destroys it.
    /// </summary>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        ref var entityData = ref this.Owner.GetEntityData(this);
        // save copy to local var for protect from cleanup fields outside.
        EcsEntity savedEntity = default;
        savedEntity.Id = this.Id;
        savedEntity.Gen = this.Gen;
        savedEntity.Owner = this.Owner;
#if DEBUG
        if (entityData.Gen != this.Gen) throw new Exception("Cant touch destroyed entity.");
#endif
        // remove components first.
        for (var i = entityData.ComponentsCountX2 - 2; i >= 0; i -= 2)
        {
            savedEntity.Owner.UpdateFilters(-entityData.Components[i], savedEntity, entityData);
            savedEntity.Owner.ComponentPools[entityData.Components[i]].Recycle(entityData.Components[i + 1]);
            entityData.ComponentsCountX2 -= 2;
#if DEBUG
            for (var ii = 0; ii < savedEntity.Owner.DebugListeners.Count; ii++)
                savedEntity.Owner.DebugListeners[ii].OnComponentListChanged(savedEntity);
#endif
        }

        entityData.ComponentsCountX2 = 0;
        savedEntity.Owner.RecycleEntityData(savedEntity.Id, ref entityData);
#if DEBUG
        for (var ii = 0; ii < savedEntity.Owner.DebugListeners.Count; ii++)
            savedEntity.Owner.DebugListeners[ii].OnEntityDestroyed(savedEntity);
#endif
    }

    /// <summary>
    ///     Is entity null-ed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNull()
    {
        return this.Id == 0 && this.Gen == 0;
    }

    /// <summary>
    ///     Is entity alive. If world was destroyed - false will be returned.
    /// </summary>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive()
    {
        if (!IsWorldAlive()) return false;
        ref var entityData = ref this.Owner.GetEntityData(this);
        return entityData.Gen == this.Gen && entityData.ComponentsCountX2 >= 0;
    }

    /// <summary>
    ///     Is world alive.
    /// </summary>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWorldAlive()
    {
        return this.Owner != null && this.Owner.IsAlive();
    }

    /// <summary>
    ///     Gets components count on entity.
    /// </summary>
#if ENABLE_IL2CPP
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetComponentsCount()
    {
        ref var entityData = ref this.Owner.GetEntityData(this);
#if DEBUG
        if (entityData.Gen != this.Gen) throw new Exception("Cant touch destroyed entity.");
#endif
        return entityData.ComponentsCountX2 <= 0 ? 0 : entityData.ComponentsCountX2 >> 1;
    }

    /// <summary>
    ///     Gets types of all attached components.
    /// </summary>
    /// <param name="entity">Entity.</param>
    /// <param name="list">List to put results in it. if null - will be created. If not enough space - will be resized.</param>
    /// <returns>Amount of components in list.</returns>
    public int GetComponentTypes(ref Type[] list)
    {
        ref var entityData = ref this.Owner.GetEntityData(this);
#if DEBUG
        if (entityData.Gen != this.Gen) throw new Exception("Cant touch destroyed entity.");
#endif
        var itemsCount = entityData.ComponentsCountX2 >> 1;
        if (list == null || list.Length < itemsCount) list = new Type[itemsCount];
        for (int i = 0, j = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2, j++)
            list[j] = this.Owner.ComponentPools[entityData.Components[i]].ItemType;
        return itemsCount;
    }

    /// <summary>
    ///     Gets values of all attached components as copies. Important: force boxing / unboxing!
    /// </summary>
    /// <param name="entity">Entity.</param>
    /// <param name="list">List to put results in it. if null - will be created. If not enough space - will be resized.</param>
    /// <returns>Amount of components in list.</returns>
    public int GetComponentValues(ref IEcsComponent[] list)
    {
        ref var entityData = ref this.Owner.GetEntityData(this);
#if DEBUG
        if (entityData.Gen != this.Gen) throw new Exception("Cant touch destroyed entity.");
#endif
        var itemsCount = entityData.ComponentsCountX2 >> 1;
        if (list == null || list.Length < itemsCount) list = new IEcsComponent[itemsCount];
        for (int i = 0, j = 0, iMax = entityData.ComponentsCountX2; i < iMax; i += 2, j++)
            list[j] = this.Owner.ComponentPools[entityData.Components[i]].GetItem(entityData.Components[i + 1]);
        return itemsCount;
    }
}