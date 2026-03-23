<template>
  <div v-if="hasBingo" class="bingo-celebration-area">
    <div class="bingo-celebration" role="alert" aria-live="polite">
      🎉 BINGO! 🎉
    </div>
    
    <div class="share-section">
      <p class="share-prompt">Share your win!</p>
      
      <div class="share-buttons">
        <button @click="shareToX" class="share-btn share-btn--x" aria-label="Share to X (Twitter)">
          <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor"><path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/></svg>
          <span>Share on X</span>
        </button>
        
        <button @click="shareToBsky" class="share-btn share-btn--bsky" aria-label="Share to Bluesky">
          <svg viewBox="0 0 600 530" width="18" height="18" fill="currentColor"><path d="m135.72 44.03c66.496 49.921 138.02 151.14 164.28 205.46 26.262-54.316 97.782-155.54 164.28-205.46 47.98-36.021 125.72-63.892 125.72 24.795 0 17.712-10.155 148.79-16.111 170.07-20.703 73.984-96.144 92.854-163.25 81.433 117.3 19.964 147.14 86.092 82.697 152.22-122.39 125.59-175.91-31.511-189.63-71.766-2.514-7.3797-3.6904-10.832-3.7077-7.8964-0.0174-2.9357-1.1937 0.51669-3.7077 7.8964-13.72 40.255-67.24 197.36-189.63 71.766-64.444-66.128-34.605-132.26 82.697-152.22-67.108 11.421-142.55-7.4491-163.25-81.433-5.9562-21.282-16.111-152.36-16.111-170.07 0-88.687 77.742-60.816 125.72-24.795z"/></svg>
          <span>Share on Bluesky</span>
        </button>
        
        <button @click="shareToLinkedIn" class="share-btn share-btn--linkedin" aria-label="Share to LinkedIn">
          <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor"><path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/></svg>
          <span>Share on LinkedIn</span>
        </button>
      </div>
      
      <button @click="downloadAndShare" class="share-btn share-btn--download" aria-label="Download board image for sharing">
        <i class="bi bi-download"></i>
        <span>📸 Download Board Image to Attach</span>
      </button>
    </div>
  </div>
</template>

<script>
export default {
  name: 'BingoCelebrationArea',
  props: {
    hasBingo: {
      type: Boolean,
      default: false
    }
  },
  emits: ['download-image'],
  data() {
    return {
      shareText: "I got BINGO at #AspireConf! 🎉 Come meet the new Aspire → conf.aspire.dev",
      shareUrl: "https://conf.aspire.dev"
    }
  },
  methods: {
    shareToX() {
      const url = `https://twitter.com/intent/tweet?text=${encodeURIComponent(this.shareText)}`
      window.open(url, '_blank', 'noopener,noreferrer')
    },
    
    shareToBsky() {
      const url = `https://bsky.app/intent/compose?text=${encodeURIComponent(this.shareText)}`
      window.open(url, '_blank', 'noopener,noreferrer')
    },
    
    shareToLinkedIn() {
      const url = `https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(this.shareUrl)}`
      window.open(url, '_blank', 'noopener,noreferrer')
    },
    
    downloadAndShare() {
      this.$emit('download-image')
    }
  }
}
</script>

<style scoped>
.bingo-celebration-area {
  text-align: center;
}

.share-section {
  margin-top: 1rem;
}

.share-prompt {
  font-size: 0.9rem;
  font-weight: 600;
  margin-bottom: 0.75rem;
  color: var(--text-color, #fff);
  opacity: 0.9;
}

.share-buttons {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}

.share-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.6rem 1rem;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  color: #fff;
}

.share-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}

.share-btn--x {
  background: #000;
}

.share-btn--x:hover {
  background: #333;
}

.share-btn--bsky {
  background: #0085ff;
}

.share-btn--bsky:hover {
  background: #0066cc;
}

.share-btn--linkedin {
  background: #0077B5;
}

.share-btn--linkedin:hover {
  background: #005582;
}

.share-btn--download {
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  width: 100%;
}

.share-btn--download:hover {
  background: linear-gradient(135deg, #4f46e5, #7c3aed);
}
</style>
