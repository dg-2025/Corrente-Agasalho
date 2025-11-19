-- =============================================================
-- SCRIPT DE CRIAÇÃO DO BANCO DE DADOS "CORRENTE DO AGASALHO"
-- DIALETO: PostgreSQL
-- VERSÃO 2.0 (Com lógica de Quantidade/Lote)
-- =============================================================

-- 1. CRIAÇÃO DO SCHEMA (Boa Prática)
CREATE SCHEMA IF NOT EXISTS corrente_agasalho;

-- Define o schema como padrão para esta sessão
SET search_path TO corrente_agasalho;

-- =============================================================
-- 2. TABELAS DE PARÂMETROS (Gerenciadas pelo Admin)
-- =============================================================

-- RF03, RF04: Armazena Categorias, Pontos e flag de Peça Essencial
CREATE TABLE IF NOT EXISTS param_categorias (
    id_categoria INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome VARCHAR(100) NOT NULL UNIQUE,
    pontos_doacao INT NOT NULL DEFAULT 0,
    pontos_retirada INT NOT NULL DEFAULT 0,
    is_essencial BOOLEAN NOT NULL DEFAULT FALSE
);

-- RF03: Armazena os Tamanhos (Ex: P, M, G, Infantil)
CREATE TABLE IF NOT EXISTS param_tamanhos (
    id_tamanho INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome VARCHAR(50) NOT NULL UNIQUE
);

-- RF03: Armazena as Condições (Ex: Novo, Usado (Bom estado))
CREATE TABLE IF NOT EXISTS param_condicoes (
    id_condicao INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome VARCHAR(50) NOT NULL UNIQUE
);

-- RF09: Armazena o destino da triagem (Ex: Disponível, Para Reparo)
CREATE TABLE IF NOT EXISTS param_status_triagem (
    id_status INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome VARCHAR(50) NOT NULL UNIQUE
);

-- =============================================================
-- 3. TABELAS DE ENTIDADE (Cadastros Principais)
-- =============================================================

-- RF01, RF05: Armazena os logins de acesso ao sistema
CREATE TABLE IF NOT EXISTS usuarios (
    id_usuario INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome_login VARCHAR(100) NOT NULL UNIQUE,
    senha_hash TEXT NOT NULL, -- RNF05: (TEXT para hashes longos)
    nivel_acesso VARCHAR(50) NOT NULL, -- (Ex: "Administrador", "Funcionário")
    is_ativo BOOLEAN NOT NULL DEFAULT TRUE
);

-- RF02: Armazena os Pontos de Coleta
CREATE TABLE IF NOT EXISTS pontos_coleta (
    id_ponto_coleta INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    responsavel VARCHAR(255),
    telefone VARCHAR(20),
    cep VARCHAR(9), -- RF14
    rua VARCHAR(255),
    numero VARCHAR(20),
    complemento VARCHAR(100),
    bairro VARCHAR(100),
    cidade VARCHAR(100),
    uf VARCHAR(2),
    is_ativo BOOLEAN NOT NULL DEFAULT TRUE
);

-- RF06: Armazena Doadores E Beneficiários
CREATE TABLE IF NOT EXISTS pessoas (
    id_pessoa INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    documento VARCHAR(20) UNIQUE, -- (CPF ou outro)
    telefone VARCHAR(20),
    saldo_pontos INT NOT NULL DEFAULT 0, -- RF08, RF10
    is_vulneravel BOOLEAN NOT NULL DEFAULT FALSE, -- RF06
    cep VARCHAR(9), -- RF14
    rua VARCHAR(255),
    numero VARCHAR(20),
    complemento VARCHAR(100),
    bairro VARCHAR(100),
    cidade VARCHAR(100),
    uf VARCHAR(2),
    is_ativo BOOLEAN NOT NULL DEFAULT TRUE
);

-- =============================================================
-- 4. TABELAS DE TRANSAÇÃO (O Coração do Sistema)
-- (VERSÃO 2.0 - COM LÓGICA DE QUANTIDADE)
-- =============================================================

-- RF07: Log de Entradas (Cabeçalho da Doação)
CREATE TABLE IF NOT EXISTS doacoes (
    id_doacao INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_pessoa INT NOT NULL REFERENCES pessoas(id_pessoa),
    id_ponto_coleta INT NOT NULL REFERENCES pontos_coleta(id_ponto_coleta),
    data_doacao TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    pontos_gerados INT NOT NULL DEFAULT 0, -- RF08
    status_transacao VARCHAR(50) NOT NULL DEFAULT 'Ativo' -- RF16 (Ex: "Ativo", "Estornado")
);

-- RF10, RF11: Log de Saídas (Cabeçalho da Entrega)
CREATE TABLE IF NOT EXISTS entregas (
    id_entrega INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_pessoa INT NOT NULL REFERENCES pessoas(id_pessoa),
    data_entrega TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    tipo_entrega VARCHAR(50) NOT NULL, -- (Ex: "Troca", "Doação Pura")
    pontos_debitados INT NOT NULL DEFAULT 0,
    status_transacao VARCHAR(50) NOT NULL DEFAULT 'Ativo' -- RF16 (Ex: "Ativo", "Estornado")
);

-- RF12: O INVENTÁRIO (Cada linha é um LOTE de peças de roupa)
-- (MUDANÇA CRÍTICA: Lógica de Quantidade)
CREATE TABLE IF NOT EXISTS itens (
    id_item INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    
    -- De onde o item veio (RF07)
    id_doacao INT NOT NULL REFERENCES doacoes(id_doacao),
    
    -- Características (RF03)
    id_categoria INT NOT NULL REFERENCES param_categorias(id_categoria),
    id_tamanho INT NOT NULL REFERENCES param_tamanhos(id_tamanho),
    id_condicao INT NOT NULL REFERENCES param_condicoes(id_condicao),
    
    -- Status (RF09)
    id_status_triagem INT NOT NULL REFERENCES param_status_triagem(id_status),
    
    -- LÓGICA DE QUANTIDADE (NOVO)
    quantidade_doada INT NOT NULL DEFAULT 1,
    quantidade_estoque INT NOT NULL DEFAULT 1 
    
    -- A COLUNA "id_entrega" FOI REMOVIDA
);

-- NOVA TABELA (NECESSÁRIA PARA A LÓGICA DE QUANTIDADE)
-- Armazena o "recibo" da saída (Ex: A Entrega 5 levou 2 peças do Lote 101)
CREATE TABLE IF NOT EXISTS entrega_itens (
    id_entrega_item INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_entrega INT NOT NULL REFERENCES entregas(id_entrega),
    id_item INT NOT NULL REFERENCES itens(id_item),
    quantidade_retirada INT NOT NULL
);


-- =============================================================
-- 5. TABELA DE SISTEMA
-- =============================================================

-- RF17: Armazena o "Modo de Alerta de Frio"
CREATE TABLE IF NOT EXISTS configuracoes (
    config_chave VARCHAR(100) PRIMARY KEY,
    config_valor TEXT
);

-- =============================================================
-- 6. INSERÇÃO DE DADOS INICIAIS (Defaults)
-- =============================================================

-- RF17: Garante que o Modo de Alerta exista
INSERT INTO configuracoes (config_chave, config_valor)
VALUES ('MODO_ALERTA_FRIO', 'INATIVO')
ON CONFLICT (config_chave) DO NOTHING;

-- Garante os status básicos da triagem (RF09)
INSERT INTO param_status_triagem (nome)
VALUES ('Pendente'), ('Disponível'), ('Para Reparo'), ('Descartado'), ('Entregue')
ON CONFLICT (nome) DO NOTHING;

-- Garante um usuário 'admin' inicial (RF05)
-- Senha padrão = "admin" (O Hash é para "admin")
INSERT INTO usuarios (nome_login, senha_hash, nivel_acesso, is_ativo)
VALUES ('admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 'Administrador', TRUE)
ON CONFLICT (nome_login) DO NOTHING;