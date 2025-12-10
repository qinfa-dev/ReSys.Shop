# Plan: Image Embedding Model Comparison Architecture

## Objective
To implement a modular architecture for image embedding generation and similarity search, allowing for the comparison of different State-of-the-Art (SOTA) models (OpenCLIP, DINOv2, BLIP-2) for fashion image search and recommendations. This plan outlines the selected models and the conceptual architecture for their integration and evaluation within the existing .NET and FastAPI ecosystem.

## Selected SOTA Models for Comparison

The following models will be integrated and compared to evaluate their performance for fashion image search:

1.  **Baseline/Default: OpenCLIP (e.g., `ViT-H-14-laion2b`)**
    *   **Type**: Vision Transformer-based, Multimodal (Image-Text Contrastive Learning).
    *   **Key Characteristics**: Strong general-purpose visual and semantic similarity, excellent zero-shot capabilities. Already specified in `ProductImageConfiguration.cs`.
    *   **Relevance**: Core model for image-to-image and future text-to-image search.

2.  **Comparison 1: DINOv2 (e.g., `ViT-L/14`)**
    *   **Type**: Vision Transformer-based, Self-supervised learning (Self-Distillation with No Labels).
    *   **Key Characteristics**: Produces highly robust and general visual features without needing human-labeled data. Excels at pure visual similarity tasks.
    *   **Relevance**: Provides a strong baseline for purely visual feature extraction, useful for comparing against multimodal approaches.

3.  **Comparison 2: BLIP-2 (e.g., `flan_t5_xxl`)**
    *   **Type**: Advanced Vision-Language Model (VLM), connecting frozen image encoders with large language models.
    *   **Key Characteristics**: SOTA in vision-language understanding (image captioning, visual question answering). Offers deep semantic integration between vision and text.
    *   **Relevance**: Explores more advanced multimodal capabilities beyond CLIP, potentially leading to richer semantic search and understanding.

## Conceptual Architecture for Image-Based Search & Model Comparison

The integration and comparison will follow a modular architecture, primarily leveraging FastAPI for ML inference and .NET for orchestration and data management.

### 1. Image Ingestion & Preprocessing
*   **Client Upload**: Users upload query images via the frontend (e.g., Vue.js).
*   **.NET API Reception**: The `ReSys.API` receives the image upload.
*   **Preprocessing**: Images are consistently resized, normalized, and formatted to meet the input requirements of each specific embedding model.

### 2. Embedding Generation (FastAPI ML Service)
*   **Dedicated FastAPI Service**: A FastAPI application (e.g., `ReSys.EmbeddingService.py`) will host the embedding models.
*   **Modular Endpoints**:
    *   `POST /embed/{model_name}`: This endpoint will accept an image (file upload or URL) and a `model_name` (e.g., `openclip`, `dinov2`, `blip2`).
    *   **Dynamic Model Loading**: All comparison models will be loaded into memory during the FastAPI application startup to ensure low inference latency.
    *   **Inference**: The service preprocesses the image and passes it through the specified `model_name` to generate its high-dimensional vector embedding.
    *   **Return Embedding**: The generated embedding (e.g., 1024-dimensional float array) is returned to the .NET caller.

### 3. Vector Storage & Indexing
*   **Product Embedding Generation**: During product ingestion or as a batch process, all product images in the catalog will have embeddings generated for *each* comparison model (OpenCLIP, DINOv2, BLIP-2).
*   **Database Schema Update**: The `ReSys.Core.Domain.Catalog.Products.Images.ProductImage` entity will be extended or associated with a new entity to store multiple embeddings per image, each tagged with its `model_name`.
    *   **Option A (Simpler for Demo)**: Add separate columns to `ProductImage`: `EmbeddingOpenCLIP`, `EmbeddingDinoV2`, `EmbeddingBLIP2` (all `vector(DIMS)`).
    *   **Option B (More Scalable)**: Create a new `ProductImageEmbedding` entity/table: `(Id, ProductImageId, ModelName, Embedding, EmbeddingGeneratedAt)`.
*   **Vector Database**: The chosen vector database (`Pgvector` in PostgreSQL) will store these embeddings.
*   **Indexing**: HNSW indexes will be applied to each embedding column/field for efficient similarity search (e.g., `ix_product_images_embedding_openclip_hnsw`, `ix_product_images_embedding_dinov2_hnsw`).

### 4. Similarity Search (.NET Orchestration & Vector DB Query)
*   **.NET Orchestration**: The `ReSys.API` backend will:
    *   Receive the query image from the frontend.
    *   Call the FastAPI `/embed/{model_name}` endpoint to get the query image's embedding, based on the user's selected comparison model.
    *   Construct an Entity Framework Core query that utilizes `Pgvector`'s similarity operators (e.g., `<=>` for cosine distance) to search the relevant embedding column (`EmbeddingOpenCLIP`, `EmbeddingDinoV2`, etc.) in the `ProductImage` table.
    *   Retrieve the IDs of the top K (e.g., 5-10) most similar `ProductImage` entities.

### 5. Post-processing & Retrieval
*   **Product Details Fetch**: Using the retrieved product IDs, the .NET backend will query its relational database for full product details (name, description, price, product images, etc.).
*   **Result Presentation**: The rich product information is sent back to the client/frontend for display.

### 6. Demonstration & Evaluation Setup
*   **Frontend Model Selector**: The frontend will include a user interface element (e.g., a dropdown or radio buttons) allowing the user to select which embedding model (OpenCLIP, DINOv2, BLIP-2) to use for the current image search query.
*   **Side-by-Side Comparison**: The search results could be displayed side-by-side for different models or allow easy toggling between results from different models for qualitative visual comparison.
*   **Metrics for Offline Evaluation (Optional for Demo)**: For a more rigorous evaluation, a curated dataset with ground-truth similar items would be used to calculate metrics like Precision@K, Recall@K, and Mean Average Precision (mAP) for each model.