# Reinventing the wheel

## Disclaimer

Provided without any warranty.

## Contents

### Hashing functions

The well tested implementations of the following hash functions are provided as a bonus:

* RIPEMD-160
* SHA-256, SHA-224
* SHA-512, SHA-384, SHA-512/256, SHA-512-224
* SHA3-512, SHA3-384, SHA3-256
* Keccak-512, Keccak-384, Keccak-256

### HMAC

* HMAC-SHA-224, HMAC-SHA-256
* HMAC-SHA-512_224, HMAC-SHA-512_256, HMAC-SHA-384, HMAC-SHA-512

### Key derivation

* The generic implementation of PBKDF2 which is able to work on top of all HMAC hashers

### Elliptic cryptography

* The reimplementation of uECC (limited by the secp256k1 subset):
    * Key generation
    * Message signing
    * Signature verification
    * ECDH
    * Private and public key tweak by scalar

### Symmetric cryptography

* AES256-CTR with PKCS7 padding

### MISC

* ShamirUtil for secret sharing with optional master password

## API

All hashers are implementing the well known Init - Update - Finalize paradighm. 

### Hashing

The hash function implementations are derived from the `IHasher` interface which is declaring the following method signatures:

```csharp
/// Reset the hasher's internal state
public void Reset()

/// Write bytes to hasher state
public void Update(ReadOnlySpan<byte> input)

/// Write hash into provided span or array
//  Note: This implies the Reset() operation
public void Digest(Span<byte> hash)

/// This property returns size of the 
// result buffer for the Digest() call
public int HashSz;
```

### HMAC

The HMAC implementations are derived from `IMac` interface which is defining these signatures:

```csharp
/// Initialize or re-initialize hasher with a given key
///  Note: It will overwrite any existing state
public void Init(ReadOnlySpan<byte> key);

/// Write bytes to hasher state
public void Update(ReadOnlySpan<byte> input);

/// Reset the hasher to the post-initialized state
public void Reset();

/// Write hash into provided span or array
///  Note: This implies the Reset() operation
public void Digest(Span<byte> hash);

/// Reset hasher to non-initialized state
///  All internal structures will be either zeroed or set to their standard defined initial values
public void Dispose();

/// This property returns size of the buffer
// which you need to provide for the Digest() call
public int HashSz;
```

The only available implementations are currently limited by the SHA2 based ones. The `HMAC_SHA224`, `HMAC_SHA256`, `HMAC_SHA384` and `HMAC_SHA512` structures are residing in the `Wheel.Hashing.HMAC.SHA2` namespace.

## Notable features

All hashers are implemented as structures of the deterministic size, which is designed to be known at the compile time. This allows you to allocate them on stack. This can be done either directly, using `stackalloc` operator, or indirectly by letting the compiler to decide. Stack allocated structures are avoiding the garbage collecting overhead since their living cycle is limited by that of the function's stack memory. The fact that they're not placed in the CLR heap is a good thing as well, especially for security.
