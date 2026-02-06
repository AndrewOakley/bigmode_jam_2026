shader_type canvas_item;

// MUST assign a FastNoiseLite to this in the Inspector!
uniform sampler2D noise_img : repeat_enable, filter_linear; 
uniform vec4 flame_color : source_color = vec4(1.0, 0.5, 0.2, 1.0);
uniform vec4 edge_color : source_color = vec4(1.0, 0.1, 0.0, 1.0);
uniform float speed : hint_range(0.1, 5.0) = 1.0;

// Adjust this to match your bar's red color
uniform vec4 target_color : source_color = vec4(1.0, 0.0, 0.0, 1.0); 
uniform float sensitivity : hint_range(0.0, 1.0) = 0.5;

void fragment() {
    // 1. Get the current pixel color
    vec4 current_pixel = texture(TEXTURE, UV);
    
    // 2. Calculate how close this pixel is to our "Red" target
    float diff = distance(current_pixel.rgb, target_color.rgb);
    
    // 3. If it's NOT red (like the black border) or it's transparent, leave it alone
    if (diff > sensitivity || current_pixel.a < 0.1) {
        COLOR = current_pixel;
    } else {
        // 4. Create the movement using SCREEN_UV (most reliable for StyleBoxes)
        // This forces the noise to move even if the bar's UVs are broken
        vec2 moving_uv = SCREEN_UV * 2.0; // Scaled up to see noise better
        moving_uv.y -= TIME * speed;
        
        // 5. Generate noise value (0.0 to 1.0)
        float n = texture(noise_img, moving_uv).r;
        
        // 6. Blend the fire colors based on the noise
        vec3 fire_effect = mix(edge_color.rgb, flame_color.rgb, n);
        
        COLOR = vec4(fire_effect, current_pixel.a);
    }
}