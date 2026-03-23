<template>
  <div class="aspire-callout glass-card">
    <h3>New to Aspire?</h3>
    <p>Streamline your dev loop with Aspire!</p>
    <a href="https://conf.aspire.dev/" 
       target="_blank" 
       rel="noopener noreferrer" 
       class="btn btn--secondary btn--link">
      Learn More at conf.aspire.dev
    </a>
    
    <div class="social-links">
      <span>Follow us:</span>
      <a href="https://www.youtube.com/watch?v=6j61K9Sna2M" 
         target="_blank" 
         rel="noopener noreferrer" 
         aria-label="YouTube Live Stream">
        <i class="bi bi-youtube social-icon"></i>
      </a>
      <a href="https://www.twitch.tv/aspiredotdev" 
         target="_blank" 
         rel="noopener noreferrer" 
         aria-label="Twitch">
        <i class="bi bi-twitch social-icon"></i>
      </a>
      <a href="https://aka.ms/aspire-discord" 
         target="_blank" 
         rel="noopener noreferrer" 
         aria-label="Discord">
        <i class="bi bi-discord social-icon"></i>
      </a>
      <a href="https://github.com/dotnet/aspire" 
         target="_blank" 
         rel="noopener noreferrer" 
         aria-label="GitHub">
        <i class="bi bi-github social-icon"></i>
      </a>
    </div>
  </div>
</template>

<script>
export default {
  name: 'AspireCallout'
}
</script>
