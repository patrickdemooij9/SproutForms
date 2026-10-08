/**
 * Looks a handler up by its key, such as a validator by rule type or an outcome handler by outcome type.
 */
export interface Lookup<T> {
    get(key: string): T | undefined;
}

/**
 * Handlers by key that fall back to a parent's. The module-level register functions fill the global registries; give each app,
 * or each request on a server, a child of those so what it registers doesn't leak into the others.
 */
export class Registry<T> implements Lookup<T> {
    private readonly items = new Map<string, T>();

    constructor(private readonly parent?: Lookup<T>) {}

    register(key: string, item: T): void {
        this.items.set(key, item);
    }

    get(key: string): T | undefined {
        return this.items.get(key) ?? this.parent?.get(key);
    }
}
